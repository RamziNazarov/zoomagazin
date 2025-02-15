﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZooMag.Controllers;
using ZooMag.Data;
using ZooMag.DTOs.Order;
using ZooMag.Entities;
using ZooMag.Mapping;
using ZooMag.Models;
using ZooMag.Models.ViewModels.Carts;
using ZooMag.Models.ViewModels.Orders;
using ZooMag.Models.ViewModels.Products;
using ZooMag.Services.Interfaces;
using ZooMag.ViewModels;

namespace ZooMag.Services
{
    public class OrdersService : IOrdersService
    {
        private readonly ApplicationDbContext _context;
        // private readonly IMapper _mapper;


        public OrdersService(ApplicationDbContext context, ICartsService cartsService)
        {
            _context = context;
            // _mapper = new MapperConfiguration(x => x.AddProfile<GeneralProfile>()).CreateMapper();
        }


        public async Task<Response> CreateAsync(CreateOrderRequest request,int userId)
        {
            var order = new Order
            {
                Address = request.Address,
                City = request.City,
                Comment = request.Comment,
                Email = request.Email,
                DeliveryTime = request.DeliveryTime,
                OrderDate = DateTime.Now,
                PhoneNumber = request.PhoneNumber,
                AdditionalPhoneNumber = request.AdditionalPhoneNumber,
                SecondAdditionalPhoneNumber = request.SecondAdditionalPhoneNumber,
                PickupPointId = request.PickupPointId,
                OrderStatusId = 1,
                PaymentMethodId = request.PaymentMethodId,
                OrderProductItems = request.ProductItemIds.Select(x => new OrderProductItem
                {
                    ProductItemId = x
                }).ToList(),
                UserId = userId,
                DeliveryTypeId = request.DeliveryTypeId,
                UserName = request.UserName
            };

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            return new Response {Status = "success", Message = "Успешно"};
        }

        public async Task<Response> UpdateOrderStatusAsync(UpdateOrderStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(request.OrderId);
            if (order == null)
                return new Response
                {
                    Message = "Не найден",
                    Status = "error"
                };
            order.OrderStatusId = request.StatusId;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return new Response
            {
                Message = "Успешно",
                Status = "success"
            };
        }

        public async Task<List<OrderResponse>> GetUserOrders(int userId)
        {
            return await _context.Orders
                .Where(x => x.UserId == userId)
                .Include(x=>x.OrderStatus)
                .Include(x=>x.PickupPoint)
                .Include(x => x.OrderProductItems)
                .ThenInclude(x => x.ProductItem)
                .Select(x => new OrderResponse
                {
                    Id = x.Id,
                    Status = x.OrderStatus.Title,
                    DeliveryAddress = x.PickupPointId.HasValue ? x.PickupPoint.Name : x.Address,
                    OrderDate = x.OrderDate,
                    Summa = x.OrderProductItems.Sum(pi=>Math.Round(pi.ProductItem.Price - pi.ProductItem.Price * pi.ProductItem.Percent / 100, 2))
                }).ToListAsync();
        }
    }
}
/*

public async Task<int> Count()
{
    return await _context.Orders.CountAsync();
}

public async Task<Response> Create(InpOrderModel orderModel, string userKey)
{
    try
    {
        if (orderModel.carts == null || orderModel.carts.Count() == 0)
        {
            return new Response { Status = "error", Message = "Корзина пуста!" };
        }
        if (orderModel.PhoneNumber.ToString().Length != 9)
        {
            return new Response { Status = "error", Message = "Неверный номер телефон!" };
        }
        List<Cart> carts = new List<Cart>();
        foreach(var cart in orderModel.carts)
        {
            var p = await _context.Products.FindAsync(cart.ProductId);
            if (p == null)
                continue;
            carts.Add(new Cart
            {
                ProductId = cart.ProductId,
                Price = cart.Quantity<=1?p.IsSale?p.SellingPrice:p.OriginalPrice:p.IsSale?cart.Quantity*p.SellingPrice:cart.Quantity*p.OriginalPrice,
                SizeId = cart.SizeId,
                Quantity = cart.Quantity<=1?1:cart.Quantity
            });
        }
        var order = new Order
        {
            UserKey = userKey,
            PhoneNumber = orderModel.PhoneNumber,
            DeliveryType = orderModel.DeliveryType == 1 ? "Доставка" : "Самовывоз",
            DeliveryAddress = orderModel.DeliveryAddress,
            PaymentMethodId = orderModel.PayMethodId,
            OrderStatusId = 1,
            OrderSumm = carts.Sum(p => p.Price),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _context.Orders.Add(order);
        await Save();
        foreach (var cart in carts)
        {
            _context.OrderItems.Add(
                new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cart.ProductId,
                    Price = cart.Price,
                    SizeId = cart.SizeId,
                    Quantity = cart.Quantity
                });
        }
        await Save();
        return new Response { Status = "success", Message = "Заказ успешно оформлен!" };
    }
    catch (Exception ex)
    {
        return new Response { Status = "error", Message = ex.Message };
    }

}

public async Task<List<Order>> FetchMyOrders(string userKey)
{
    var orders = await _context.Orders.Where(p => p.UserKey == userKey).ToListAsync();
    foreach (var order in orders)
    {
        order.OrderStatus = await _context.OrderStatuses.FindAsync(order.OrderStatusId);
        order.PaymentMethod = await _context.PaymentMethods.FindAsync(order.PaymentMethodId);
        order.PaymentMethod.Orders = null;
        order.OrderStatus.Orders = null;
        //order.OrderItems = await _context.OrderItems.Where(p => p.OrderId == order.Id).ToListAsync();
    }
    return orders;
}

public async Task<List<Order>> FetchAll(int offset, int limit)
{
    var orders = await _context.Orders.Skip(offset).Take(limit).ToListAsync();
    foreach (var order in orders)
    {
        order.OrderStatus = await _context.OrderStatuses.FindAsync(order.OrderStatusId);
        order.PaymentMethod = await _context.PaymentMethods.FindAsync(order.PaymentMethodId);
        order.PaymentMethod.Orders = null;
        order.OrderStatus.Orders = null;
        //order.OrderItems = await _context.OrderItems.Where(p => p.OrderId == order.Id).ToListAsync();
    }

    return orders;
}

public async Task<Response> SetSize(int orderitemid, int sizeid)
{
    var orderitem = await _context.OrderItems.FindAsync(orderitemid);
    if (orderitem == null)
    {
        return new Response { Status = "error", Message = "Не найден!" };
    }
    var productsize = await _context.ProductSizes
        .FirstOrDefaultAsync
        (
        p => p.ProductId == orderitem.ProductId &&
        p.SizeId == sizeid
        );
    if (productsize == null)
    {
        return new Response { Status = "error", Message = "Размер не найден!" };
    }

    orderitem.SizeId = sizeid;
    await Save();
    return new Response { Status = "success", Message = "Размер успешно присвоен!" };
}

private async Task<int> Save()
{
    return await _context.SaveChangesAsync();
}

public async Task<Response> Delete(int id)
{
    var order = await _context.Orders.FindAsync(id);
    if(order==null)
    {
        return new Response { Status = "error", Message = "Не найден!" };
    }
    _context.OrderItems.RemoveRange(
        await _context.OrderItems.Where(
            p=>p.OrderId == order.Id).ToListAsync());

    _context.Orders.Remove(order);
    await Save();
    return new Response { Status = "success", Message = "Успешно удален!" };
}

public async Task<List<OrderStatus>> FetchStatuses()
{
    return await _context.OrderStatuses.ToListAsync();
}


public async Task<Response> ChangeStatus(int id, int statusid)
{
    var order = await _context.Orders.FindAsync(id);
    if(order==null)
    {
        return new Response {Status = "error" ,Message = "Заказ не найден!" };
    }
    var status = await _context.OrderStatuses.FindAsync(statusid);
    if(status==null)
    {
        return new Response {Status = "error", Message = "Статус не найден!" };
    }
    order.OrderStatusId = statusid;
    await Save();
    return new Response {Status = "success",Message = "Статус успешно присвоен!"};
}

public async Task<Response> DeleteItem(int id)
{
    var orderitem = await _context.OrderItems.FindAsync(id);
    if (orderitem == null)
    {
        return new Response { Status = "error", Message = "Не найден!" };
    }
    int count = await _context.OrderItems.CountAsync(p => p.ProductId == orderitem.ProductId);
    if (count > 1)
    {
        _context.OrderItems.Remove(orderitem);
        await Save();
        var order = await _context.Orders.FindAsync(orderitem.OrderId);
        order.OrderSumm = _context.OrderItems.Where(p => p.OrderId == orderitem.OrderId).Select(p => p.Price).Sum();
        await Save();
    }
    return new Response {Status = "success",Message = "Успешно удален!" };
}

public async Task<decimal> IncrQty(int itemid)
{
    var orderitem = await _context.OrderItems.FindAsync(itemid);
    if (orderitem == null)
    {
        return 0;
    }
    var productPrice = (await _context.Products.FindAsync(orderitem.ProductId)).SellingPrice;

    orderitem.Quantity++;
    orderitem.Price = productPrice * orderitem.Quantity;
    await Save();
    var order = await _context.Orders.FindAsync(orderitem.OrderId);
    if (order == null)
    {
        return 0;
    }
    order.OrderSumm = _context.OrderItems.Where(p => p.OrderId == orderitem.OrderId).Select(p => p.Price).Sum();
    await Save();
    return orderitem.Price;
}

public async Task<decimal> DecrQty(int itemid)
{
    var orderitem = await _context.OrderItems.FindAsync(itemid);
    if (orderitem == null)
    {
        return 0;
    }
    if (orderitem.Quantity > 1)
    {
        var productPrice = (await _context.Products.FindAsync(orderitem.ProductId)).SellingPrice;
        orderitem.Quantity--;
        orderitem.Price = productPrice * orderitem.Quantity;
        await Save();
        var order = await _context.Orders.FindAsync(orderitem.OrderId);
        if (order == null)
        {
            return 0;
        }
        order.OrderSumm = _context.OrderItems.Where(p => p.OrderId == orderitem.OrderId).Select(p => p.Price).Sum();
        await Save();
    }
    return orderitem.Price;
}

public async Task<OutOrderModel> FetchDetail(int orderid)
{
    var order = await _context.Orders.FindAsync(orderid);
    OutOrderModel orderModel = new OutOrderModel();
    if (order!=null)
    {
        orderModel = _mapper.Map<Order, OutOrderModel>(order);
        orderModel.OrderStatus = await _context.OrderStatuses.FindAsync(order.OrderStatusId);
        orderModel.OrderStatus.Orders = null;
        orderModel.PaymentMethod = await _context.PaymentMethods.FindAsync(order.PaymentMethodId);
        orderModel.PaymentMethod.Orders = null;
        orderModel.OrderItems = new List<OrderItemModel>();
        foreach (var item in await _context.OrderItems.Where(p => p.OrderId == orderid).ToListAsync())
        {
            Product product = await _context.Products.FindAsync(item.ProductId);
            OutProductModel productModel = new OutProductModel();
            if (product!=null)
            {
                productModel = _mapper.Map<Product, OutProductModel>(product);
                productModel.Images = _mapper.Map<List<ProductGalery>, List<ProductImagesModel>>(
                    await _context.ProductGaleries.Where(p => p.ProductId == productModel.Id)
                    .ToListAsync());
                productModel.Sizes = await FetchSizesByProductId(productModel.Id);
            }
            Size size = await _context.Sizes.FindAsync(item.SizeId);
            if(size!=null)
            {
                size.ProductSizes = null;
            }

            orderModel.OrderItems.Add(new OrderItemModel
            {
                Id = item.Id,
                Price = item.Price,
                Quantity = item.Quantity,
                Product = productModel,
                Size = size
            });

        }
        return orderModel;
    }
    return null;
}

private async Task<List<SizeModel>> FetchSizesByProductId(int productId)
{
    List<Size> sizes = new List<Size>();
    List<ProductSize> ProductSize = await _context.ProductSizes.Where(p => p.ProductId == productId).ToListAsync();
    foreach (var item in ProductSize)
    {
        Size size = await _context.Sizes.Where(s => s.Id == item.SizeId).FirstOrDefaultAsync();
        if (size != null)
            sizes.Add(size);
    }
    return _mapper.Map<List<Size>, List<SizeModel>>(sizes); ;
}

}
}
*/