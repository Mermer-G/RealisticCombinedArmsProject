public interface IFuel 
{
    string Name { get; }

    float PowerPerUnit { get; }

    IFuel subfuel { get; }
    /// <summary>
    /// Multiply with Fuel to get Oxygen, divide with Oxygen to get Fuel
    /// </summary>
    float? oxidizerFuelRatio { get; }

}

class Oxygen : IFuel
{
    public string Name => "Oxygen";
    public float PowerPerUnit => 0;
    public IFuel subfuel => null;
    public float? oxidizerFuelRatio => null;
}

class JP8 : IFuel
{
    public string Name => "JP-8";
    public float PowerPerUnit => 42.8f;
    public IFuel subfuel => new Oxygen();
    public float? oxidizerFuelRatio => 2.74f;
}