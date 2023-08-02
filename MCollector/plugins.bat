xcopy /y ..\MCollector.Plugins.Prometheus\bin\%1\net6.0\*.dll .\bin\%1\net6.0\Plugins\Prometheus\
del /f .\bin\%1\net6.0\Plugins\Prometheus\MCollector.Core.dll