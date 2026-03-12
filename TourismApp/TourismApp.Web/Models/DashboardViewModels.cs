using System.ComponentModel.DataAnnotations;

namespace TourismApp.Web.Models;

public class DashboardViewModel
{
    public int TotalPlaces { get; set; }
    public int ApprovedPlaces { get; set; }
    public int TotalVisitsThisMonth { get; set; }
    public double AvgRating { get; set; }
    public List<PlaceViewModel> Places { get; set; } = [];
}