public interface IFuelProvider
{
    IFuelTank FuelTank { get; }
    IFuel fuelType { get; }
    bool EnableFlow { get; set; }
    float FlowRate { get; }
}
