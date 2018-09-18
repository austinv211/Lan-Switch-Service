using System.ServiceProcess;


namespace COSDLanSwitchService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            ServiceBase[] ServicesToRun = new ServiceBase[] { new COSDLanSwitchService(args) };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
