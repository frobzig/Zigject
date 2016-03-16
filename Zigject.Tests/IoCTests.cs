using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Zigject.Tests
{
    [TestClass]
    public class IoCTests
    {
        public interface IVehicle
        {
            int Capacity { get; set; }
            List<string> Passengers { get; }
        }

        public class VehicleBase : IVehicle
        {
            public int Capacity { get; set; }
            public List<string> Passengers { get; protected set; }
        }

        public class Unicycle : VehicleBase
        {
            public Unicycle()
            {
                this.Capacity = 1; //// unless you are clowns
            }
        }

        public class Car : VehicleBase
        {
            public Car(int capacity = 5)
            {
                this.Capacity = capacity;
            }
        }

        public class Jet : VehicleBase
        {
            public static async Task<Jet> Create(int capacity = 10)
            {
                return await Task.FromResult<Jet>(new Jet() { Capacity = capacity });
            }
        }

        public class Shuttle : VehicleBase
        {
            public Shuttle(int capacity = 56, params string[] passengers)
            {
                this.Capacity = capacity;
                this.Passengers = passengers.ToList();
            }
        }

        public class TukTuk : VehicleBase
        {
            public static TukTuk Create(int capacity = 3, params string[] args)
            {
                return new TukTuk(capacity, args);
            }

            protected TukTuk(int capacity = 3, params string[] passengers)
            {
                this.Capacity = capacity;
                this.Passengers = passengers.ToList();
            }
        }

        public class Rikshaw : VehicleBase
        {
            public Rikshaw(int capacity = 3, params string[] passengers)
            {
                this.Capacity = capacity;
                this.Passengers = passengers.ToList();
            }
        }

        [TestMethod]
        public void TypeActivatorTest()
        {
            IoC container = new IoC();

            container.Register<IVehicle>(typeof(Car));

            IVehicle vehicle = container.Get<IVehicle>();

            Assert.IsTrue(vehicle is Car);
            Assert.AreEqual<int>(5, vehicle.Capacity);
        }

        [TestMethod]
        public void TypeActivatorWithParamsTest()
        {
            IoC container = new IoC();

            container.Register<IVehicle>(typeof(Car));

            IVehicle vehicle = container.GetWithArgs<IVehicle>(17);

            Assert.IsTrue(vehicle is Car);
            Assert.AreEqual<int>(17, vehicle.Capacity);
        }

        [TestMethod]
        public void SingletonTest()
        {
            IoC container = new IoC();

            IVehicle circusCycle = new Unicycle() { Capacity = 4 };
            container.Register<IVehicle>(circusCycle);

            IVehicle vehicle = container.GetWithArgs<IVehicle>();

            Assert.IsTrue(vehicle is Unicycle);
            Assert.AreSame(circusCycle, vehicle);
            Assert.AreEqual<int>(4, vehicle.Capacity);
        }

        [TestMethod]
        public void LazySingletonTest()
        {
            IoC container = new IoC();

            container.Register<IVehicle>(typeof(Car), IoC.InjectionBehavior.LazySingleton);

            IVehicle vehicle1 = container.GetWithArgs<IVehicle>(6);
            IVehicle vehicle2 = container.GetWithArgs<IVehicle>(10);
            IVehicle vehicle3 = container.GetWithArgs<IVehicle>();

            Assert.IsTrue(vehicle1 is Car);
            Assert.IsTrue(vehicle2 is Car);
            Assert.IsTrue(vehicle3 is Car);
            Assert.AreSame(vehicle1, vehicle2);
            Assert.AreSame(vehicle1, vehicle3);
            Assert.AreEqual<int>(6, vehicle1.Capacity);

            //// should be 6 because it was only instantiated the first time
            Assert.AreEqual<int>(6, vehicle2.Capacity);
            Assert.AreEqual<int>(6, vehicle3.Capacity);
        }

        [TestMethod]
        public void RegisterOverwritesTest()
        {
            IoC container = new IoC();

            IVehicle cycle = new Unicycle() { Capacity = 4 };

            container.Register<IVehicle>(cycle);
            Assert.IsInstanceOfType(container.Get<IVehicle>(), typeof(Unicycle));

            IVehicle car = new Car() { Capacity = 1 };
            container.Register<IVehicle>(car);
            Assert.IsInstanceOfType(container.Get<IVehicle>(), typeof(Car));
        }

        [TestMethod]
        public void CreateMethodTest()
        {
            IoC container = new IoC();

            container.Register<IVehicle>(typeof(Jet), IoC.InjectionBehavior.CreateMethod);

            IVehicle vehicle1 = container.GetWithArgs<IVehicle>();
            IVehicle vehicle2 = container.GetWithArgs<IVehicle>(16);
            IVehicle vehicle3 = container.GetWithArgs<IVehicle>(300);
            IVehicle vehicle4 = container.GetWithArgs<IVehicle>(2);

            Assert.AreNotSame(vehicle2, vehicle1);
            Assert.AreNotSame(vehicle3, vehicle2);
            Assert.AreNotSame(vehicle3, vehicle1);
            Assert.AreNotSame(vehicle4, vehicle3);

            Assert.AreEqual<int>(10, vehicle1.Capacity);
            Assert.AreEqual<int>(16, vehicle2.Capacity);
            Assert.AreEqual<int>(300, vehicle3.Capacity);
            Assert.AreEqual<int>(2, vehicle4.Capacity);
        }

        [TestMethod]
        public async Task CreateMethodAsyncTest()
        {
            IoC container = new IoC();

            await container.RegisterAsync<IVehicle>(typeof(Jet), IoC.InjectionBehavior.CreateMethod);

            IVehicle vehicle1 = await container.GetWithArgsAsync<IVehicle>(2);
            IVehicle vehicle2 = await container.GetWithArgsAsync<IVehicle>(16);
            IVehicle vehicle3 = await container.GetWithArgsAsync<IVehicle>(300);

            Assert.AreNotSame(vehicle2, vehicle1);
            Assert.AreNotSame(vehicle3, vehicle2);
            Assert.AreNotSame(vehicle3, vehicle1);

            Assert.AreEqual<int>(2, vehicle1.Capacity);
            Assert.AreEqual<int>(16, vehicle2.Capacity);
            Assert.AreEqual<int>(300, vehicle3.Capacity);
        }

        [TestMethod]
        public async Task CreateMethodLazyAsyncTest()
        {
            IoC container = new IoC();

            await container.RegisterAsync<IVehicle>(typeof(Jet),
                IoC.InjectionBehavior.CreateMethod | IoC.InjectionBehavior.LazySingleton);

            IVehicle vehicle1 = await container.GetWithArgsAsync<IVehicle>(2);
            IVehicle vehicle2 = await container.GetWithArgsAsync<IVehicle>(16);
            IVehicle vehicle3 = await container.GetWithArgsAsync<IVehicle>(300);

            Assert.AreSame(vehicle2, vehicle1);
            Assert.AreSame(vehicle3, vehicle2);
            Assert.AreSame(vehicle3, vehicle1);

            Assert.AreEqual<int>(2, vehicle1.Capacity);
            Assert.AreEqual<int>(2, vehicle2.Capacity);
            Assert.AreEqual<int>(2, vehicle3.Capacity);
        }

        [TestMethod]
        public void CreateMethodLazyTest()
        {
            IoC container = new IoC();

            container.Register<IVehicle>(typeof(Jet),
                IoC.InjectionBehavior.CreateMethod | IoC.InjectionBehavior.LazySingleton);

            IVehicle vehicle1 = container.GetWithArgs<IVehicle>(2);
            IVehicle vehicle2 = container.GetWithArgs<IVehicle>(16);
            IVehicle vehicle3 = container.GetWithArgs<IVehicle>(300);

            Assert.AreSame(vehicle2, vehicle1);
            Assert.AreSame(vehicle3, vehicle2);
            Assert.AreSame(vehicle3, vehicle1);

            Assert.AreEqual<int>(2, vehicle1.Capacity);
            Assert.AreEqual<int>(2, vehicle2.Capacity);
            Assert.AreEqual<int>(2, vehicle3.Capacity);
        }

        [TestMethod]
        public void OptionalsAndVarArgsTest()
        {
            IoC container = new IoC();

            container.Register<IVehicle>(typeof(Shuttle));

            IVehicle vehicle1 = container.GetWithArgs<IVehicle>(Type.Missing, "Bubbles", "Ricky", "Julian", "Bobandy");
            Shuttle shuttle = vehicle1 as Shuttle;

            Assert.IsNotNull(shuttle);
            Assert.AreEqual<int>(56, shuttle.Capacity);
            Assert.AreEqual<int>(4, shuttle.Passengers.Count);
            Assert.AreEqual<string>("Bubbles", shuttle.Passengers[0]);
            Assert.AreEqual<string>("Ricky", shuttle.Passengers[1]);
            Assert.AreEqual<string>("Julian", shuttle.Passengers[2]);
            Assert.AreEqual<string>("Bobandy", shuttle.Passengers[3]);
        }

        [TestMethod]
        public void CreateOptionalsAndVarArgsTest()
        {
            IoC container = new IoC();

            container.Register<IVehicle>(typeof(TukTuk), IoC.InjectionBehavior.CreateMethod);

            IVehicle vehicle1 = container.GetWithArgs<IVehicle>(Type.Missing, "Bubbles", "Ricky", "Julian", "Bobandy");
            TukTuk tuktuk = vehicle1 as TukTuk;

            Assert.IsNotNull(tuktuk);
            Assert.AreEqual<int>(3, tuktuk.Capacity);
            Assert.AreEqual<int>(4, tuktuk.Passengers.Count);
            Assert.AreEqual<string>("Bubbles", tuktuk.Passengers[0]);
            Assert.AreEqual<string>("Ricky", tuktuk.Passengers[1]);
            Assert.AreEqual<string>("Julian", tuktuk.Passengers[2]);
            Assert.AreEqual<string>("Bobandy", tuktuk.Passengers[3]);

            IVehicle vehicle2 = container.GetWithArgs<IVehicle>(Type.Missing);
            TukTuk moonbuggy = vehicle1 as TukTuk;
        }

        [TestMethod]
        public void GetWithCreateOptionalsAndVarArgsTest()
        {
            Assert.Fail("Filling out optional parameters for constructors does not work yet (and this is disabled until they do).");

            IoC container = new IoC();

            container.Register<IVehicle>(typeof(TukTuk), IoC.InjectionBehavior.CreateMethod);

            IVehicle vehicle1 = container.Get<IVehicle>();
            TukTuk tuktuk = vehicle1 as TukTuk;

            Assert.IsNotNull(tuktuk);
            Assert.AreEqual<int>(3, tuktuk.Capacity);
            Assert.AreEqual<int>(0, tuktuk.Passengers.Count);
        }

        [TestMethod]
        public void GetWithOptionalsAndVarArgsTest()
        {
            Assert.Fail("Filling out optional parameters for constructors does not work yet.");

            IoC container = new IoC();

            container.Register<IVehicle>(typeof(Rikshaw));

            IVehicle vehicle1 = container.Get<IVehicle>();
            Rikshaw rikshaw = vehicle1 as Rikshaw;

            Assert.IsNotNull(rikshaw);
            Assert.AreEqual<int>(3, rikshaw.Capacity);
            Assert.AreEqual<int>(0, rikshaw.Passengers.Count);
        }
    }
}
