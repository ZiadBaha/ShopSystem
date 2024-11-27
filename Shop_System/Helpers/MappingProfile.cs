using AutoMapper;
using ShopSystem.Core.Dtos.Program;
using ShopSystem.Core.Models.Entites;

namespace Shop_System.Helpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {

            CreateMap<Category, CategoryDTO>().ReverseMap();

            CreateMap<Customer, CustomerDTO>().ReverseMap();
            CreateMap<Customer, CreateCustomerDTO>().ReverseMap();
            CreateMap<Customer, UpdateCustomerDTO>().ReverseMap();

            CreateMap<Expense, ExpenseDTO>().ReverseMap();

           
            CreateMap<Merchant, MerchantDTO>()
            .ForMember(dest => dest.TotalPurchaseAmount, opt => opt.MapFrom(src => src.TotalPurchaseAmount))
            .ForMember(dest => dest.TotalOutstandingBalance, opt => opt.MapFrom(src => src.TotalOutstandingBalance));

            CreateMap<Merchant, CreateMerchantDTO>().ReverseMap();

            CreateMap<Merchant, UpdateMerchantDTO>().ReverseMap();


            CreateMap<Payment, PaymentDTO>().ReverseMap();
            CreateMap<ProductDTO, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()); 
            CreateMap<Product, ProductDTO>();


            CreateMap<Purchase, PurchaseDTO>().ReverseMap();
            CreateMap<PurchaseItem, PurchaseItemDTO>().ReverseMap();
            CreateMap<CreatePurchaseDTO, Purchase>();
            CreateMap<CreatePurchaseItemDTO, PurchaseItem>();


            CreateMap<Order, OrderDTO>()
               .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems));


            CreateMap<OrderItem, OrderItemDTO>()
                .ForMember(dest => dest.SubTotal, opt => opt.MapFrom(src => src.Quantity * src.Product.SellingPrice));

            CreateMap<CreateOrderDTO, Order>()
                .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems));

            CreateMap<OrderItemDTO, OrderItem>();


        }

    }
}
