# Zigject
Increasingly complex IoC Container, which can still be used in a very simple form. 

As of version 1.3.6, every method has an async counterpart.  The async methods are preferred.  If you need to use a sync method, it will spin up a new thread and wait for the result.

##### Below are some simple examples.  There are a few more options for specifying default values and instantiation actions, but at this point it would be easier to refer to the extremely small source file and unit tests.
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

#### Examples with Non-Standard Behavior
### Class Setup  
```
public SomeAPIClient : IAPIClient
{
    public static async Task<SomeAPIClient> Create(params object[] args)
    {
        string connectionString = await ConnectionStringEngine.GetAsync();
        return 
    }
    
    private readonly string _connectionString;
    
    private SomeAPIClient(string connectionString)
    {
        this._connectionString = connectionString;
    }
}
```

### Register a Type to Instantiate with Create Method
```
IoC container = new IoC();

container.RegisterAsync<IAPIClient>(typeof(SomeAPIClient), IoC.InjectionBehavior.CreateMethod);

IAPIClient cliient = container.GetAsync<IAPIClient>(); //// each call will create a new instance
```

### Register a Type to Instantiate with Create Method and LazySingleton
```
IoC container = new IoC();

//// when using LazySingleton, you must register a type, not an instance
container.RegisterAsync<IAPIClient>(typeof(SomeAPIClient), IoC.InjectionBehavior.CreateMethod | IoC.InjectionBehavior.LazySingleton);

IAPIClient client1 = container.GetAsync<IAPIClient>(); //// the first call will create the instance
IAPIClient client2 = container.GetAsync<IAPIClient>(); //// this will return the same instance created by the first call
```