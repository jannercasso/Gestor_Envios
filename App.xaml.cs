using System;
using System.Configuration;
using System.Data;
using System.Threading;
using System.Windows;

namespace GestorEnvios;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        bool esNuevaInstancia;
        _mutex = new Mutex(true, "GestorEnviosProcterMutex", out esNuevaInstancia);

        if (!esNuevaInstancia)
        {
            MessageBox.Show("La aplicación ya está en ejecución.", "Gestor de Envíos Procter",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        base.OnExit(e);
    }
}