using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Common;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Contracts.Responses.Transaction;

namespace Ombor.API.Controllers;

/// <summary>
/// Endpoints to manage products and inventory.
/// </summary>
[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    /// <summary>
    /// Retrieves a list of products, with optional filtering by search term, category, and price range.
    /// </summary>
    /// <param name="request">Filtering and paging parameters.</param>
    /// <returns>Paged lisf of <see cref="ProductDto"/>.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ProductDto[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedList<ProductDto>>> GetAsync(
        [FromQuery] GetProductsRequest request)
    {
        var response = await productService.GetAsync(request);

        Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(response.MetaData));

        return Ok(response);
    }

    /// <summary>
    /// Retrieves a specific product by its unique ID.
    /// </summary>
    /// <param name="request">Request containing the product ID.</param>
    /// <returns>The matching <see cref="ProductDto"/>.</returns>
    [HttpGet("{Id:int:min(1)}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductByIdAsync(
        [FromRoute] GetProductByIdRequest request)
    {
        var response = await productService.GetByIdAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves product transactions by product ID.
    /// </summary>
    /// <param name="request">Request containing the product ID.</param>
    /// <returns>The transactions of the product.</returns>
    [HttpGet("{Id:int:min(1)}/transactions")]
    [ProducesResponseType(typeof(ProductTransactionDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductTransactionDto[]>> GetProductTransactionsByIdAsync(
        [FromRoute] GetProductTransactionsRequest request)
    {
        var response = await productService.GetTransactionsAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="request">Payload describing the product to create.</param>
    /// <returns>The created product details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<CreateProductResponse>> PostAsync(
        [FromForm] CreateProductRequest request)
    {
        await Task.Delay(4000);
        var response = await productService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetProductByIdAsync),
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id"> The ID of the product to update.</param>
    /// <param name="request">Payload describing the product to update (must include the ID of the product).</param>
    /// <returns>The updated product details.</returns>
    [HttpPut("{id:int:min(1)}")]
    [ProducesResponseType(typeof(UpdateProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateProductResponse>> PutAsync(
        [FromRoute] int id,
        [FromForm] UpdateProductRequest request)
    {
        await Task.Delay(4000);
        if (id != request.Id)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "ID mismatch",
                Detail = $"Route ID ({id}) does not match body ID ({request.Id}).",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = await productService.UpdateAsync(request);

        return Ok(response);
    }

    /// <summary>
    /// Deletes a product by its ID.
    /// </summary>
    /// <param name="request">Delete request containing the product ID.</param>
    [HttpDelete("{id:int:min(1)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] DeleteProductRequest request)
    {
        await productService.DeleteAsync(request);

        return NoContent();
    }
}
