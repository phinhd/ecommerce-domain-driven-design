using System.ComponentModel.DataAnnotations;
using ECommerce.Application.DTOs.Common;

namespace ECommerce.Application.DTOs.Product;

/// <summary>Query-string parameters for listing products.</summary>
public sealed class ProductQuery
{
    [MaxLength(100)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, Pagination.MaxPageSize)]
    public int PageSize { get; set; } = Pagination.DefaultPageSize;

    /// <summary>Only products created by the current user.</summary>
    public bool Mine { get; set; }

    /// <summary>Hide products created by the current user (useful for a storefront).</summary>
    public bool ExcludeMine { get; set; }
}
