namespace Watts.Azure.Utils.Interfaces.DataFactory
{
    public interface IDataFactoryEnvironment
    {
        string GetDataFactoryStorageAccountConnectionString();

        string GetConnectionStringConsumptionStorage();

        string GetConnectionStringConsumptionForecastsStorage();

        string GetConnectionStringManualReadingsStorage();

        string GetConnectionStringInstallationsStorage();

        string GetConnectionStringWeatherStorage();
    }
}