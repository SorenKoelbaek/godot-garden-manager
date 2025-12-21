using Serilog;
using Serilog.Sinks.GodotConsole;
using Godot;

public partial class LoggerManager : Node
{
    public override void _Ready()
    {
        // Initialize Serilog with Godot console sink
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.GodotConsole(
                outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();
        
        Log.Information("Logger initialized");
    }
    
    public override void _ExitTree()
    {
        // Clean up logger on exit
        Log.CloseAndFlush();
    }
}
