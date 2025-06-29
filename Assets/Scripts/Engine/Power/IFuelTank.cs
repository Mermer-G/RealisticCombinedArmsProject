public interface IFuelTank
{
    IFuel FuelType { get; }
    float MaxFuelAmount { get; }
    float FuelAmount { get; set; }
}