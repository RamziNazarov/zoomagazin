﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZooMag.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public bool IsDelivery { get; set; }
        public int UserId { get; set; }
        public int? DeliveryTypeId { get; set; }
        public int OrderStatusId { get; set; }
        public string PhoneNumber { get; set; }
        public string AdditionalPhoneNumber { get; set; }
        public string SecondAdditionalPhoneNumber { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime OrderDate { get; set; }
        public string City { get; set; }
        public int? PickupPointId { get; set; }
        public DateTime DeliveryTime { get; set; }
        public string Comment { get; set; }
        public int PaymentMethodId { get; set; }
        public virtual User User { get; set; }
        public virtual OrderStatus OrderStatus { get; set; }
        public virtual DeliveryType DeliveryType { get; set; }
        public virtual PaymentMethod PaymentMethod { get; set; }
        public virtual PickupPoint PickupPoint { get; set; }
        public virtual ICollection<OrderProductItem> OrderProductItems { get; set; }
    }
}
