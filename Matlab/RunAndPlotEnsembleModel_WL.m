clear all
close all

%addpath('R:\Research Communities\SurrogateModelling\Matlab\Source\SmMatlabGit\SmMatlabGit\Utilities')
NET.addAssembly('H:\SyncPC\csharp\DtuSmModels\EnsembleModels\bin\Release\EnsembleModels.dll');
myDtuSmObj = NET.addAssembly('H:\SyncPC\csharp\DtuSmModels\EnsembleModels\bin\Release\DtuSmModels.dll');
         

Nenseble = 100;
NdaysToRun = 10;
t=0:1:NdaysToRun*60*24;
%dwf=0.3+0.2*sin(2*pi*t/(60*24));

k=0.01;
N = length(t);
truePrecip = gamrnd(k,1/k,N,1);
% noise = truePrecip .* (2*betarnd(1,1,N,1) - 1);
measuredPrecip = truePrecip .* betarnd(1,1,N,1) * 2;


truth_model = DtuSmModels.MainModel();
ensmodel = EnsembleModels.EnsembleHandler();
pathToPrmFile = 'H:\SyncPC\csharp\DtuSmModels\EnsembleModelsTestData\SlowAndFast_WL_output_ensemble.prm';
%pathToPrmFile = 'H:\SyncPC\csharp\DtuSmModels\TestData\DAtests\SlowAndFast_WL_output.prm';
pathToPrmFile_out = 'H:\SyncPC\csharp\DtuSmModels\TestData\DAtests\temp.prm';
pathToPrmFile_Truth = 'H:\SyncPC\csharp\DtuSmModels\TestData\DAtests\SlowAndFast_truth.prm';

truth_model.initializeFromFile(pathToPrmFile_Truth);
truth_model.setRainDataForAllCatchments(truePrecip);
%[conn1,index1]= findConnection(truth_model, 'SM4', 'outlet1');
%conns = truth_model.getConnections();

ensmodel.InitializeFromFile(pathToPrmFile, Nenseble);
ensmodel.Mat_AddTempCorrNoiseOnRain( 0.01, 60 * 100, 60, 3);%(variance, decorrelationTimeTau, dt, truncateLimitInStds
ensmodel.SetRainDataForAllCatchments(truePrecip);

%model.addOutputVariable(DtuSmModels.outputType.GlobalVolumen);
truth_model.addOutputVariable('outlet1');
%ensmodel.Mat_AddOutputVariable(DtuSmModels.outputType.GlobalVolumen, '',true, false, true, true,   false);

%ensmodel.Mat_AddOutputVariable(xxxhandle.GetEnumValues().Get(3), 'outlet1', true, true, true, true,   false);%0 = LinkFlowTs; 2 = GlobalVolume; 3 = OutletFlow
%ensmodel.Mat_AddOutputVariable(DtuSmModels.outputType.outletFlowTimeSeries, 'outlet1',true, true, true, true,   false);

%ensmodel.AddDA_variable(xxxhandle.GetEnumValues().Get(3), 'outlet1', "","");
%tic, ensmodel.runForOneMinuteRainInput(), toc;

ensmodel.Mat_perturbeParameters(0.2,2);
ensmodel.Mat_StateDaMethod('EnKF',0,0,0);
ensmodel.Mat_SetParameterDaMethod('EnKF',0,0,0);
%ensmodel.Mat_SetParameterDaMethod('EnKFsmartInflation',0,0,0);
tic
truth_model.runForOneMinuteRainInput();
xx = truth_model.output.dataCollection.ToArray;
tt_true = truth_model.output.timeInSeconds.ToArray;
resx = xx(1);
trueVals = resx.data.ToArray.double;

for i=1:N
   %ensmodel.SetRainDataForAllCatchments([0 0 0 0 0 ]);
   ensmodel.StepModels();
%i
   %ensmodel.Mat_perturbeParameters(0.0001,2);%(std, truncationLimInStd)
    if(~mod(i,1)) %only update params every xth step
       % ensmodel.Mat_updateParamsToObs(100, 10.1^2, 1.0, 0.1);%(value, var, inflationFactor)
    end
    
   % ensmodel.Mat_updateToObs(trueVals(i), 0.1^2, 1.00, 1.0 );%(value, var, inflationFactor) 
    ensmodel.Mat_updateToObs(100, 0.01^2, 1.00, 0.50 );%(value, var, inflationFactor) 
end
toc
%*********************
%calculate std og coef of var to see effect of parameter update. 
for i=1:Nenseble
   prm = ensmodel.models(i).getParameters();
   for j=1:prm.Length    
    prmx(j,i) = prm(j);
   end
end

std(prmx')./mean(prmx')
%(mean(prmx')-median(prmx'))./std(prmx')
%*************************

%model.runForOneMinuteRainInput();
xx = ensmodel.outputCollection.hydraulicOutput.ToArray;
tt = ensmodel.outputCollection.timeInSeconds.ToArray;

figure();
values = xx(1).dataSeries(1).data.ToArray.double;%mean
plot(tt.double/3600, values, '-b' ); hold on
%ylim([0 0.5]);
stdData = xx(1).dataSeries(2).data.ToArray.double;%std
plot(tt.double/3600, values-stdData, '--b' ); 
plot(tt.double/3600, values+stdData, '--b' ); 

values = xx(1).dataSeries(3).data.ToArray.double;%min
plot(tt.double/3600, values,'-g' ); hold on
values = xx(1).dataSeries(4).data.ToArray.double;%max
plot(tt.double/3600, values,'-g' ); hold on

plot(tt.double/3600, trueVals,'-r' ); hold on

estimated_model = DtuSmModels.MainModel();
estimated_model.initializeFromFile(pathToPrmFile);
estimated_model.setParameter(mean(prmx'));
estimated_model.saveModelParameters(pathToPrmFile_out);

truth_model.output.resetOutputSeries();
ensmodel.outputCollection.resetOutputSeries();
