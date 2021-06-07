clear all
close all

NET.addAssembly('H:\SyncPC\csharp\DtuSmModels\EnsembleModels\bin\Release\EnsembleModels.dll');
myDtuSmObj = NET.addAssembly('H:\SyncPC\csharp\DtuSmModels\EnsembleModels\bin\Release\DtuSmModels.dll');     

NdaysToRun = 10;
t=0:1:NdaysToRun*60*24;

k=0.01;
N = length(t);
truePrecip = gamrnd(k,1/k,N,1);
truth_model = DtuSmModels.MainModel();

pathToPrmFile = 'H:\SyncPC\csharp\DtuSmModels\TestData\DAtests\SlowAndFast_WL_output.prm';
truth_model.initializeFromFile(pathToPrmFile);
truth_model.setRainDataForAllCatchments(truePrecip);

truth_model.runForOneMinuteRainInput();
xx = truth_model.output.dataCollection.ToArray;
tt = truth_model.output.timeInSeconds.ToArray;

resx = xx(2);values = resx.data.ToArray.double;plot(tt.double/3600, values,'-r' ); hold on
resx = xx(3);values = resx.data.ToArray.double;plot(tt.double/3600, values,'--g' ); hold on
truth_model.output.resetOutputSeries();

