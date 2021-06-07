%************** MB *************
% creates the precipitation and measurements ts

k=0.01;
N = 1000;

truePrecip = gamrnd(k,1/k,N,1);
% noise = truePrecip .* (2*betarnd(1,1,N,1) - 1);
measuredPrecip = truePrecip .* betarnd(1,1,N,1) * 2;
%measuredPrecip = truePrecip ;
figure; plot(truePrecip,'-b'); hold on; plot(measuredPrecip,'-r');