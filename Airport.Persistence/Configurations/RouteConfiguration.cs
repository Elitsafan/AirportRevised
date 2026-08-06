namespace Airport.Persistence.Configurations
{
    internal class RouteConfiguration
    {
        public async Task ConfigureAsync(IMongoClient client, IOptions<AirportDbConfiguration> dbConfiguration)
        {
            var routesCollection = client
                .GetDatabase(dbConfiguration.Value.DatabaseName)
                .GetCollection<Route>(dbConfiguration.Value.RoutesCollectionName);

            var data = new List<Route>
            {
                new()
                {
                    RouteId = new ObjectId("650abb1ee574435a814d7ec0"),
                    RouteName = "Landing",
                    Directions = new List<Direction>
                    {
                        new()
                        {
                            From = new ObjectId("000000000000000000000001"),
                            To = new ObjectId("000000000000000000000002"),
                        },
                        new()
                        {
                            From = new ObjectId("000000000000000000000002"),
                            To = new ObjectId("000000000000000000000003"),
                        },
                        new()
                        {
                            From = new ObjectId("000000000000000000000003"),
                            To = new ObjectId("000000000000000000000004"),
                        },
                        new()
                        {
                            From = new ObjectId("000000000000000000000004"),
                            To = new ObjectId("000000000000000000000005"),
                        },
                        new()
                        {
                            From = new ObjectId("000000000000000000000005"),
                            To = new ObjectId("000000000000000000000006"),
                        },
                        new()
                        {
                            From = new ObjectId("000000000000000000000005"),
                            To = new ObjectId("000000000000000000000007"),
                        },
                    }
                },
                new()
                {
                    RouteId = new ObjectId("650abb1ee574435a814d7ec1"),
                    RouteName = "Departure",
                    Directions = new List<Direction>
                    {
                        new()
                        {
                            From = new ObjectId("000000000000000000000006"),
                            To = new ObjectId("000000000000000000000008")
                        },
                        new()
                        {
                            From = new ObjectId("000000000000000000000007"),
                            To = new ObjectId("000000000000000000000008")
                        },
                        new()
                        {
                            From = new ObjectId("000000000000000000000008"),
                            To = new ObjectId("000000000000000000000004")
                        },
                        new()
                        {
                            From = new ObjectId("000000000000000000000004"),
                            To = new ObjectId("000000000000000000000009"),
                        },
                    }
                },
                //new Route
                //{
                //    RouteId = new ObjectId("650abb1ee574435a814d7ec2"),
                //    RouteName = "Landing",
                //    Directions = new List<Direction>
                //    {
                //        new Direction
                //        {
                //            From = new ObjectId("000000000000000000000001"),
                //            To = new ObjectId("000000000000000000000014")
                //        },
                //        new Direction
                //        {
                //            From = new ObjectId("000000000000000000000014"),
                //            To = new ObjectId("000000000000000000000004")
                //        },
                //        new Direction
                //        {
                //            From = new ObjectId("000000000000000000000004"),
                //            To = new ObjectId("000000000000000000000015"),
                //        },
                //        new Direction
                //        {
                //            From = new ObjectId("000000000000000000000015"),
                //            To = new ObjectId("000000000000000000000006"),
                //        },
                //        new Direction
                //        {
                //            From = new ObjectId("000000000000000000000015"),
                //            To = new ObjectId("000000000000000000000007"),
                //        },
                //        new Direction
                //        {
                //            From = new ObjectId("000000000000000000000015"),
                //            To = new ObjectId("000000000000000000000016"),
                //        }
                //    }
                //},
                //new Route
                //{
                //    RouteId = new ObjectId("650abb1ee574435a814d7ec3"),
                //    RouteName = "Departure",
                //    Directions = new List<Direction>
                //    {
                //        new Direction
                //        {
                //            From = new ObjectId("000000000000000000000010"),
                //            To = new ObjectId("000000000000000000000011")
                //        },
                //        new Direction
                //        {
                //            From = new ObjectId("000000000000000000000011"),
                //            To = new ObjectId("000000000000000000000012")
                //        },
                //        new Direction
                //        {
                //            From = new ObjectId("000000000000000000000012"),
                //            To = new ObjectId("000000000000000000000013")
                //        },
                //        new Direction
                //        {
                //            From = new ObjectId("000000000000000000000013"),
                //            To = new ObjectId("000000000000000000000001"),
                //        },
                //        new Direction
                //        {
                //            From = new ObjectId("000000000000000000000013"),
                //            To = new ObjectId("000000000000000000000004"),
                //        }
                //    }
                //}
            };
            await routesCollection.InsertManyAsync(data);
        }
    }
}