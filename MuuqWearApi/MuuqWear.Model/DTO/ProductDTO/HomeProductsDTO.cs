namespace MuuqWear.API.DTO.ProductDTO
{
    public class HomeProductsDTO
    {
        // 6 new arrival products
        public List<ProductDTO> NewArrivals { get; set; } = new();

        // 6 featured products
        public List<ProductDTO> Featured { get; set; } = new();

        // 6 best seller products
        public List<ProductDTO> BestSellers { get; set; } = new();
    }
}
