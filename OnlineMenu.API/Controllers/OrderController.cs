using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace OnlineMenu.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {

        private readonly ILogger<OrderController> _logger;

        public OrderController(ILogger<OrderController> logger)
        {
            _logger = logger;
        }

        [HttpPost("add")]
        public ActionResult AddOrder([FromBody] List<int> productIds)
        {
            var products = new List<Product>();
            foreach (var productId in productIds)
                products.Add(Menu.First(p => p.Id == productId));
            

            var order = new Order { Id = OrderId++, Products = products, Status = OrderStatus.ReadyToPlay };
            Orders.Add(order);
            return Ok();
        }

        [HttpGet]
        public ActionResult<List<Order>> GetOrders()
        {
            return Orders.Where(o => o.Status != OrderStatus.Done).ToList();
        }

        [HttpGet("get-menu")]
        public ActionResult<List<Product>> GetMenu()
        {
            return Menu;
        }

        [HttpGet("complete/{orderId}")]
        public ActionResult CompleteOrder([FromRoute] int orderId)
        {
            Orders.First(o => o.Id == orderId).Status = OrderStatus.Done;
            return Ok();
        }

        [HttpGet("sse")]
        public async Task GetSse(CancellationToken cancellationToken)
        {
            var response = Response;
            //Response.StatusCode = 200;

            response.Headers.Add("Content-Type", "text/event-stream");
            response.Headers.Add("Cache-Control", "no-cache");
            NotifyCollectionChangedEventHandler onMessageCreated = async (sender, eventArgs) =>
            {
                try
                {
                    var orders = sender;
                    var messageJson = JsonSerializer.Serialize(orders,
                        new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
                    await Response.WriteAsync($"data: {messageJson}\n\n");
                    await Response.Body.FlushAsync();
                }
                catch (Exception)
                {
                    // TODO: log error
                }
            };
            Orders.CollectionChanged += onMessageCreated;

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }

            Orders.CollectionChanged -= onMessageCreated;
        }

        static List<Product> Menu = new List<Product> {
            new Product { Id = 1, Name =  "Pizza", Price = 80},
            new Product { Id = 2, Name = "Fench fries", Price = 30 },
            new Product { Id = 3, Name = "Water", Price = 20},
            new Product { Id = 4, Name = "Juse", Price = 30}
        };
        static int OrderId = 0;
        static ObservableCollection<Order> Orders = new ObservableCollection<Order>();
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public List<Product> Products { get; set; }
        public OrderStatus Status { get; set; }
    }

    public enum OrderStatus
    {
        ReadyToPlay,
        InProgress,
        Done
    }

    public class OrderCreatedArgs
    {
        public Order Order { get; set; }
    }
}
