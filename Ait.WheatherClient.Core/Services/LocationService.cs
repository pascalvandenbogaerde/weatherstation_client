using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ait.WeatherClient.Core.Entities;

namespace Ait.WeatherClient.Core.Services
{
    public class LocationService
    {
        public List<Location> Locations { get; set; }
        public LocationService()
        {
            Locations = new List<Location>();
        }
    }
}
