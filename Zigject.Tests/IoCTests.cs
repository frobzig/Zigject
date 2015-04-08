using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zigject.Tests
{
    [TestClass]
    public class IoCTests
    {
        public interface IVehicle
        {
            int Capacity { get; set; }
        }

        public class VehicleBase : IVehicle
        {
            public int Capacity { get; set; }
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
            public Car()
            {
                this.Capacity = 5; 
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
        public void SingletonTest()
        {
            IoC container = new IoC();
            
            IVehicle circusCycle = new Unicycle() { Capacity = 4 };
            container.Register<IVehicle>(circusCycle);

            IVehicle vehicle = container.Get<IVehicle>();

            Assert.IsTrue(vehicle is Unicycle);
            Assert.AreSame(circusCycle, vehicle);
            Assert.AreEqual<int>(4, vehicle.Capacity);
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
    }
}
