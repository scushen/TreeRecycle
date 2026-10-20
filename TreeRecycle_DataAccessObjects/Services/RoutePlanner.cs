using System;
using System.Collections.Generic;
using System.Linq;

namespace TreeRecycle.DataAccessObjects
{
    /// <summary>
    /// Splits a collection day into trailer-sized runs that each stay in one part of the area, then orders
    /// the stops in each run.
    ///
    /// Sweep method: sort stops by compass bearing from the club, cut into runs when the trailer is full,
    /// then visit each run's stops nearest-neighbour from the club. Neighbouring bearings sit in the same wedge
    /// of the map, so a run never zig-zags across town. It is a heuristic, not an optimal route - plenty for
    /// 50-300 trees and no paid routing API needed.
    /// </summary>
    public static class RoutePlanner
    {
        public static int Plan(Club club, IList<TreeOrder> orders)
        {
            int capacity = club.TrailerCapacity > 0 ? club.TrailerCapacity : 25;
            List<TreeOrder> stops = orders.Where(o => o.IsOutstanding || o.StatusEnum == OrderStatus.Collected || o.StatusEnum == OrderStatus.NotOut)
                                          .ToList();
            if (stops.Count == 0)
                return 0;

            double originLat = (double)club.Latitude, originLng = (double)club.Longitude;

            // Start the sweep at the widest gap between neighbouring stops so a run isn't split across 0/360 degrees.
            var withBearing = stops.Select(o => new { Order = o, Bearing = Bearing(originLat, originLng, (double)o.Latitude, (double)o.Longitude) })
                                   .OrderBy(x => x.Bearing).ToList();
            int startIndex = 0;
            double widestGap = -1;
            for (int i = 0; i < withBearing.Count; i++)
            {
                double next = i + 1 < withBearing.Count ? withBearing[i + 1].Bearing : withBearing[0].Bearing + 360;
                double gap = next - withBearing[i].Bearing;
                if (gap > widestGap)
                {
                    widestGap = gap;
                    startIndex = (i + 1) % withBearing.Count;
                }
            }
            var swept = withBearing.Skip(startIndex).Concat(withBearing.Take(startIndex)).Select(x => x.Order).ToList();

            var runs = new List<List<TreeOrder>>();
            var current = new List<TreeOrder>();
            int load = 0;
            foreach (TreeOrder o in swept)
            {
                if (current.Count > 0 && load + o.Quantity > capacity)
                {
                    runs.Add(current);
                    current = new List<TreeOrder>();
                    load = 0;
                }
                current.Add(o);
                load += o.Quantity;
            }
            if (current.Count > 0)
                runs.Add(current);

            // Nearest run to the club goes first.
            runs = runs.OrderBy(r => r.Min(o => Geo.DistanceKm(club.Latitude, club.Longitude, o.Latitude, o.Longitude))).ToList();

            for (int r = 0; r < runs.Count; r++)
            {
                List<TreeOrder> ordered = NearestNeighbour(club.Latitude, club.Longitude, runs[r]);
                for (int s = 0; s < ordered.Count; s++)
                {
                    TreeOrder o = ordered[s];
                    if (o.RunNumber != r + 1 || o.RunSequence != s + 1)
                    {
                        o.RunNumber = r + 1;
                        o.RunSequence = s + 1;
                        o.Update();
                    }
                }
            }
            return runs.Count;
        }

        private static List<TreeOrder> NearestNeighbour(decimal lat, decimal lng, List<TreeOrder> run)
        {
            var remaining = new List<TreeOrder>(run);
            var result = new List<TreeOrder>();
            decimal curLat = lat, curLng = lng;
            while (remaining.Count > 0)
            {
                TreeOrder next = remaining.OrderBy(o => Geo.DistanceKm(curLat, curLng, o.Latitude, o.Longitude)).First();
                result.Add(next);
                remaining.Remove(next);
                curLat = next.Latitude;
                curLng = next.Longitude;
            }
            return result;
        }

        private static double Bearing(double lat1, double lng1, double lat2, double lng2)
        {
            double φ1 = lat1 * Math.PI / 180, φ2 = lat2 * Math.PI / 180, Δλ = (lng2 - lng1) * Math.PI / 180;
            double y = Math.Sin(Δλ) * Math.Cos(φ2);
            double x = Math.Cos(φ1) * Math.Sin(φ2) - Math.Sin(φ1) * Math.Cos(φ2) * Math.Cos(Δλ);
            return (Math.Atan2(y, x) * 180 / Math.PI + 360) % 360;
        }

        /// <summary>
        /// Google Maps directions link from the club through a run's stops. Google allows 9 waypoints in a URL,
        /// so long runs are split into several legs.
        /// </summary>
        public static IList<string> DirectionsLinks(Club club, IList<TreeOrder> run)
        {
            var links = new List<string>();
            string origin = Point(club.Latitude, club.Longitude);
            for (int i = 0; i < run.Count; i += 10)
            {
                var leg = run.Skip(i).Take(10).ToList();
                string destination = Point(leg.Last().Latitude, leg.Last().Longitude);
                string waypoints = string.Join("|", leg.Take(leg.Count - 1).Select(o => Point(o.Latitude, o.Longitude)));
                links.Add("https://www.google.com/maps/dir/?api=1&travelmode=driving&origin=" + Uri.EscapeDataString(origin)
                          + "&destination=" + Uri.EscapeDataString(destination)
                          + (waypoints == "" ? "" : "&waypoints=" + Uri.EscapeDataString(waypoints)));
                origin = destination;
            }
            return links;
        }

        private static string Point(decimal lat, decimal lng)
        {
            return lat.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + lng.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
