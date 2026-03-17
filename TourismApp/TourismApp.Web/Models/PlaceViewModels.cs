using System.ComponentModel.DataAnnotations;

namespace TourismApp.Web.Models;

public class PlaceViewModel
{
    public int PlaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Phone { get; set; }
    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalVisits { get; set; }
    public string? CategoryName { get; set; }
    public string? MainImageUrl { get; set; }
    public bool IsApproved { get; set; }
    public string? Specialty { get; set; }
    public int? PricePerPerson { get; set; }
    public string? District { get; set; }
    public bool HasParking { get; set; }
    public bool HasAircon { get; set; }
}

public class CreatePlaceViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên địa điểm")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
    public string Address { get; set; } = string.Empty;

    [Required]
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Required]
    [Range(-180, 180)]
    public double Longitude { get; set; }

    public string? Phone { get; set; }
    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
    public int? CategoryId { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
}

public class ReviewViewModel
{
    public int ReviewId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public string? OwnerReply { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PlaceId { get; set; }
    public string PlaceName { get; set; } = string.Empty;
    public byte? TasteRating { get; set; }
    public byte? PriceRating { get; set; }
    public byte? SpaceRating { get; set; }
}
