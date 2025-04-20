using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;

namespace Ombor.Application.Interfaces;

public interface IProductService
{
    Task<ProductDto[]> GetAsync(GetProductsRequest request);
    Task<ProductDto> GetByIdAsync(GetProductByIdRequest request);
    Task<CreateProductResponse> CreateAsync(CreateProductRequest request);
    Task<UpdateProductResponse> UpdateAsync(UpdateProductRequest request);
    Task DeleteAsync(DeleteProductRequest request);
}
