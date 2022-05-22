using CasaDoCodigo.Models;
using CasaDoCodigo.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace CasaDoCodigo.Repositories {

    public interface IPedidoRepository {
        Pedido GetPedido();
        void AddItem(string codigo);
        UpdateQuantidadeResponse UpdateQuantidade(ItemPedido itemPedido);
    }

    public class PedidoRepository : BaseRepository<Pedido>, IPedidoRepository {

        private readonly IHttpContextAccessor contextAcessor;
        private readonly IItemPedidoRepository itemPedidoRepository;

        public PedidoRepository(ApplicationContext contexto, IHttpContextAccessor contextAcessor, IItemPedidoRepository itemPedidoRepository) : base(contexto) {
            this.contextAcessor = contextAcessor;
            this.itemPedidoRepository = itemPedidoRepository;
        }

        public void AddItem(string codigo) {
            var produto = contexto.Set<Produto>()
                .Where(p => p.Codigo == codigo)
                .SingleOrDefault();

            if (produto == null) {
                throw new ArgumentException("Produto não encontrado!");
            }

            var pedido = GetPedido();

            var itemPedido = contexto.Set<ItemPedido>()
                .Where(i => i.Produto.Codigo == codigo && i.Pedido.Id == pedido.Id)
                .SingleOrDefault();

            if (itemPedido == null) {
                itemPedido = new ItemPedido(pedido, produto, 1, produto.Preco);
                contexto.Set<ItemPedido>().Add(itemPedido);
                contexto.SaveChanges();
            }
        }

        public Pedido GetPedido() {

            var pedidoId = GetPedidoId();
            var pedido = dbSet
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Produto)
                .Where(p => p.Id == pedidoId)
                .SingleOrDefault();

            if(pedido == null) {
                pedido = new Pedido();
                dbSet.Add(pedido);
                contexto.SaveChanges();
                SetPedidoId(pedido.Id);
            }

            return pedido;
        }

        private int? GetPedidoId(){
            return contextAcessor.HttpContext.Session.GetInt32("pedidoId");
        }

        private void SetPedidoId(int pedidoId) {
            contextAcessor.HttpContext.Session.SetInt32("pedidoId", pedidoId);
        }

        public UpdateQuantidadeResponse UpdateQuantidade(ItemPedido itemPedido) {

            var itemPedidoDb = itemPedidoRepository.GetItemPedido(itemPedido.Id);

            if (itemPedidoDb != null) {
                itemPedidoDb.AtualizaQuantidade(itemPedido.Quantidade);
                contexto.SaveChanges();

                var carrinhoViewModel = new CarrinhoViewModel(GetPedido().Itens);

                return new UpdateQuantidadeResponse(itemPedidoDb, carrinhoViewModel);
            }

            throw new ArgumentException("ItemPedido não encontrado!");
        }
    }
}
