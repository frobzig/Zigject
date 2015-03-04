# Zigject
Super Simple IoC Container
###### Below is a simple example.  There are a few more options for specifying default values and instantiation actions, but at this point it would be easier to refer to the extremely small source file.

## Example
### Class Setup
```
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
```

### Registering a Singleton
```
IoC container = new IoC();

IVehicle circusCycle = new Unicycle() { Capacity = 4 };
container.Register<IVehicle>(circusCycle);

IVehicle vehicle = container.Get<IVehicle>();
```

### Registering a Type to Instantiate on Request
```
IoC container = new IoC();

container.Register<IVehicle>(typeof(Car));

IVehicle vehicle = container.Get<IVehicle>();
```
