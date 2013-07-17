@echo off
::::::::::::::::::::::::::::::
:: Ensure environment setup
::::::::::::::::::::::::::::::
if '%BRANCH%'=='' call %~dp0env.cmd

::::::::::::::::::::::::::::::
:: Set local context
::::::::::::::::::::::::::::::
setlocal enabledelayedexpansion
pushd %~dp0..

set START_TIME=%TIME%
set TC=if '0'=='1' echo ##teamcity
set RUN=
set BUILD_RESULT=0
set SOLUTIONS=0
set SOLUTIONS_BUILT=0
set SOLUTIONS_PACKAGED=0
set DEPLOYMENTS=0
set DEPLOYMENTS_DEPLOYED=0
set DEPLOYMENT_TESTS_RUN=0
set DEPLOYMENT_TESTS_PASSED=0
set INTEGRATION_TESTS_RUN=0
set INTEGRATION_TESTS_PASSED=0
set CONFIG=%~dp0Config

:: Check if running elevated
set ELEVATED=False
AT>NUL
if %ERRORLEVEL%==0 set ELEVATED=True

::::::::::::::::::::::::::::::
:: Parse arguments
::::::::::::::::::::::::::::::
:ParseArgs
if /i not '%1'=='' (
  :: Set for TeamCity builds
  if /i '%1'=='TeamCity' (
    set TEAM_CITY=True
    set TC=echo ##teamcity
  )
  
  :: Build Configuration
  if /i '%1'=='Debug' set CONFIGURATION=Debug
  if /i '%1'=='Release' set CONFIGURATION=Release

  :: Private Configuration Path
  if /i '%1'=='PrivateConfig' (
    set PRIVATECONFIG=%~2
    shift /1
  )
  
  :: Azure Packaging and Deployment
  if /i '%1'=='Cloud' set TARGETPROFILE=Cloud
  if /i '%1'=='ApnxAppSand' set TARGETPROFILE=ApnxAppSand
  if /i '%1'=='ApnxAppProd' set TARGETPROFILE=ApnxAppProd
  if /i '%1'=='Local' set TARGETPROFILE=Local
  if /i '%1'=='Integration' set TARGETPROFILE=Integration
  if /i '%1'=='Production' set TARGETPROFILE=Production
  if /i '%1'=='Package' set PACKAGE=True
  if /i '%1'=='Deploy' (
    set DEPLOY=True
    set PACKAGE=True
  )
  if /i '%1'=='Staging' set DEPLOYMENT_SLOT=staging
  if /i '%1'=='PreserveDeployment' set DELETE_DEPLOYMENT=False
  if /i '%1'=='WebOnly' set LAND_WORKER_ROLE=False
  
  :: Sql/Storage Deployment
  if /i '%1'=='InitStorageOnly' set INIT_STORAGE_ONLY=True
  if /i '%1'=='DeploySql' set SQL_DEPLOY=True
  if /i '%1'=='CleanSql' set SQL_CLEAN=True
  if /i '%1'=='SqlServer' (
    set SQL_SERVER=%2
    shift /1
  )
  if /i '%1'=='SqlUser' (
    set SQL_USER=-u %2
    shift /1
  )
  if /i '%1'=='SqlPswd' (
    set SQL_PSWD=-p %2
    shift /1
  )
  if /i '%1'=='CreateCompany' set CREATE_DEFAULT_COMPANY=True
  if /i '%1'=='CreateAdmin' set CREATE_DEFAULT_USER=True
  if /i '%1'=='AdminUserId' (
    set DEFAULT_USER_ID=%2
    shift /1
  )
  
  :: Code Analysis
  if /i '%1'=='WarnOnStyleCop' set STYLECOP_AS_WARNINGS=True
  if /i '%1'=='WarnOnFxCop' set FXCOP_AS_WARNINGS=True
  if /i '%1'=='NoFxCop' set SKIP_FXCOP=True
  if /i '%1'=='NCover' (
    set RUN_NCOVER=True
    set CONTINUE_ON_BUILD_FAILURE=True
    set TEST_FILTER=
    set EXCLUDE_TEST_CATEGORY=
  )
  
  :: XML Documentation
  if /i '%1'=='NoXmlDoc' set XMLDOCS=False
  
  :: Test runs
  if /i '%1'=='RunBVTs' (
    set RUN_UNIT_TESTS=True
    set TEST_FILTER=UnitTests
    set EXCLUDE_TEST_CATEGORY=NonBVT
  )
  if /i '%1'=='RunIntegrationTests' (
    set RUN_INTEGRATION_TESTS=True
    set TEST_FILTER=IntegrationTests
    set EXCLUDE_TEST_CATEGORY=
  )
  if /i '%1'=='RunE2ETests' (
    set RUN_POST_DEPLOYMENT_TESTS=True
    set TEST_FILTER=E2ETests
    set EXCLUDE_TEST_CATEGORY=NonBVT
  )
  if /i '%1'=='RunAllPostDeploymentTests' (
    set RUN_POST_DEPLOYMENT_TESTS=True
    set EXCLUDE_TEST_CATEGORY=
  )
  
  :: Mock auth (for integration testing)
  if /i '%1'=='MockAuth' (
    set MOCK_AUTH=True
  )
  
  :: Specify alternate solutions to build
  if /i '%1'=='/slns' (
    shift /1
    if exist "%CONFIG%\%2" (
      set SOLUTIONS_TO_BUILD=%CONFIG%\%2
      shift /1
    ) else if exist "%2" (
      set SOLUTIONS_TO_BUILD=%2
      shift /1
    ) else (
      echo Unable to find solutions to build. The file "%2" could not be found.
      %TC%[message text='Unable to find solutions to build.' errorDetails='The file "%2" does not exist.' status='ERROR']
      goto Failed
    )
  )

  :: Only display what would be run instead of running any build commands
  if /i '%1'=='DisplayOnly' set RUN=ECHO #
  
  :: Display usage message
  if /i '%1'=='Help' goto Usage
  if /i '%1'=='?' goto Usage
  if /i '%1'=='--help' goto Usage
  if /i '%1'=='-?' goto Usage
  if /i '%1'=='/?' goto Usage
  if /i '%1'=='/help' goto Usage
  
  shift /1
  goto ParseArgs
)

::::::::::::::::::::::::::::::
:: Private Configuration
::::::::::::::::::::::::::::::
:: Default private config located in separate SVN alongside Lucy
if '%PRIVATECONFIG%'=='' set PRIVATECONFIG=%~dp0..\..\..\LucyConfig\
:: Load settings from private config
for /f "tokens=1 delims=" %%i in (%PRIVATECONFIG%\settings.txt) do set settings.%%i
:: Specific purpose config paths
set AZURECONFIG=%PRIVATECONFIG%Azure
set AZUREPROFILES=%AZURECONFIG%\Profiles

::::::::::::::::::::::::::::::
:: Default build settings
::::::::::::::::::::::::::::::
if '%TEAM_CITY%'=='' set TEAM_CITY=False
if '%TARGETPROFILE%'=='' set TARGETPROFILE=Local
if '%TARGETS%'=='' set TARGETS=Rebuild
if '%SOLUTIONS_TO_BUILD%'=='' set SOLUTIONS_TO_BUILD=%CONFIG%\solutionsToBuild.txt
if '%SOLUTIONS_TO_PACKAGE%'=='' set SOLUTIONS_TO_PACKAGE=%CONFIG%\solutionsToPackage.txt
if '%SERIAL_BUILD_SOLUTIONS%'=='' set SERIAL_BUILD_SOLUTIONS=%CONFIG%\serialBuildSolutions.txt
if '%PROJECTS_TO_DEPLOY%'=='' set PROJECTS_TO_DEPLOY=%AZURECONFIG%\projectsToDeploy.%TARGETPROFILE%.txt
if '%INTEGRATION_TESTS%'=='' set INTEGRATION_TESTS=%CONFIG%\integrationTests.txt
if '%POST_DEPLOYMENT_TESTS%'=='' set POST_DEPLOYMENT_TESTS=%CONFIG%\postDeploymentTests.txt
if '%CONFIGURATION%'=='' set CONFIGURATION=Debug
if '%STYLECOP_AS_WARNINGS%'=='' set STYLECOP_AS_WARNINGS=False
if '%FXCOP_AS_WARNINGS%'=='' set FXCOP_AS_WARNINGS=False
if '%SKIP_FXCOP%'=='' set SKIP_FXCOP=False
if '%XMLDOCS%'=='' set XMLDOCS=True
if '%RUN_UNIT_TESTS%'=='' set RUN_UNIT_TESTS=False
if '%RUN_INTEGRATION_TESTS%'=='' set RUN_INTEGRATION_TESTS=False
if '%RUN_POST_DEPLOYMENT_TESTS%'=='' set RUN_POST_DEPLOYMENT_TESTS=False
if '%RUN_NCOVER%'=='' set RUN_NCOVER=False
if '%MOCK_AUTH%'=='' set MOCK_AUTH=False
if '%CONTINUE_ON_BUILD_FAILURE%'=='' set CONTINUE_ON_BUILD_FAILURE=False
if '%DEPLOY_ON_SUCCESS%'=='' set DEPLOY_ON_SUCCESS=False
if '%PACKAGE%'=='' set PACKAGE=False
if '%DEPLOY%'=='' set DEPLOY=False
if '%DEPLOYMENT_SLOT%'=='' set DEPLOYMENT_SLOT=staging
if '%AZURECONNECTIONS%'=='' set AZURECONNECTIONS=%AZURECONFIG%\WindowsAzureConnections.xml
if '%DELETE_DEPLOYMENT%'=='' set DELETE_DEPLOYMENT=True
if '%LAND_WORKER_ROLE%'=='' set LAND_WORKER_ROLE=True
if '%INIT_STORAGE_ONLY%'=='' set INIT_STORAGE_ONLY=False
if '%SQL_DEPLOY%'=='' set SQL_DEPLOY=False
if '%SQL_CLEAN%'=='' set SQL_CLEAN=False
if '%SQL_SERVER%'=='' set SQL_SERVER=.\SQLEXPRESS
if '%SQL_TO_INSTALL%'=='' set SQL_TO_INSTALL=%CONFIG%\databasesToInstall.txt
if '%CREATE_DEFAULT_USER%'=='' set CREATE_DEFAULT_USER=False
if '%DEFAULT_USER_ID%'=='' (
  if '%TARGETPROFILE%'=='Cloud' set DEFAULT_USER_ID=%settings.DEFAULT_USER_ID_Cloud%
  if '%TARGETPROFILE%'=='ApnxAppSand' set DEFAULT_USER_ID=%settings.DEFAULT_USER_ID_ApnxAppSand%
  if '%TARGETPROFILE%'=='ApnxAppProd' set DEFAULT_USER_ID=%settings.DEFAULT_USER_ID_ApnxAppProd%
  if '%TARGETPROFILE%'=='Local' set DEFAULT_USER_ID=%settings.DEFAULT_USER_ID_Local%
  if '%TARGETPROFILE%'=='Integration' set DEFAULT_USER_ID=%settings.DEFAULT_USER_ID_Integration%
)
if '%DEFAULT_ACCESS%'=='' set DEFAULT_ACCESS=*:#:*:*
if '%DEFAULT_COMPANY_ID%'=='' set DEFAULT_COMPANY_ID=00000000000000000000000000000001
if '%DEFAULT_COMPANY_NAME%'=='' set DEFAULT_COMPANY_NAME=DefaultCompany

set START_AZURE_STORE_COMMAND="C:\Program Files\Microsoft SDKs\Windows Azure\Emulator\csrun.exe" /devstore:start
set STOP_AZURE_STORE_COMMAND="C:\Program Files\Microsoft SDKs\Windows Azure\Emulator\csrun.exe" /devstore:shutdown
set CLEAN_AZURE_STORE_COMMAND="C:\Program Files\Microsoft SDKs\Windows Azure\Emulator\devstore\dsinit.exe" /sqlinstance:%SQL_SERVER%,65000 /silent /forcecreate

set OUTDIR=%~dp0..\Public\bin\%CONFIGURATION%

if '%INIT_STORAGE_ONLY%'=='True' goto InitStorage

for /f "eol=#" %%i in (%SOLUTIONS_TO_BUILD%) do set /a SOLUTIONS=!SOLUTIONS!+1

::::::::::::::::::::::::::::::
:: Pre-build summary message
::::::::::::::::::::::::::::::
%TC%[blockOpened name='ConfigurationSummary']
echo ====================================================================================================
echo [%DATE% %TIME%] Build Started
echo.
echo Building %SOLUTIONS% solutions from %SOLUTIONS_TO_BUILD%
echo.
echo Branch:                %BRANCH%
echo Build Configuration:   %CONFIGURATION%
echo Profile:               %TARGETPROFILE%
echo Private Configuration: %PRIVATECONFIG%
echo Output:                %OUTDIR%
echo Build Packages:        %PACKAGE%
echo Target(s):             %TARGETS%
echo Solutions:             %SOLUTIONS_TO_BUILD%
echo Databases:             %SQL_TO_INSTALL%
echo Deploy:                %DEPLOY%
echo DeploySql:             %SQL_DEPLOY%
echo CleanSql:              %SQL_CLEAN%
echo Run Unit Tests:        %RUN_UNIT_TESTS%
echo Run Integration Tests: %RUN_INTEGRATION_TESTS%
if /i '%RUN_UNIT_TESTS%'=='True' (
  echo Test Assembly Name Filter: %TEST_FILTER%
  echo Exclude Test Category:     %TEST_CATEGORY%
)
echo Run Code Coverage:         %RUN_NCOVER%
echo Continue On Build Failure: %CONTINUE_ON_BUILD_FAILURE%
if /i '%DEPLOY%'=='True' (
  for /f "eol=#" %%i in (%PROJECTS_TO_DEPLOY%) do set /a DEPLOYMENTS=!DEPLOYMENTS!+1
  echo.
  echo Deploying to !DEPLOYMENTS! Cloud projects to Windows Azure from %PROJECTS_TO_DEPLOY%
  echo Delete After Deployment:   %DELETE_DEPLOYMENT%
)
echo Run Post-Deployment Tests: %RUN_POST_DEPLOYMENT_TESTS%
if /i '%RUN_POST_DEPLOYMENT_TESTS%'=='True' (
  echo     Post-Deployment Tests: %POST_DEPLOYMENT_TESTS%'
)
if /i '%TEAM_CITY%'=='True' (
  set 
  echo.
  echo TeamCity Messages Enabled
)
echo ====================================================================================================
%TC%[blockClosed name='ConfigurationSummary']
echo.

:::::::::::::::::::::::::::::::::::
:: Pre-build clean
:::::::::::::::::::::::::::::::::::
%TC%[blockOpened name='Pre-build Clean']
echo ----------------------------------------------------------------------------------------------------
echo [!DATE! !TIME!] Cleaning Public\bin folders
echo ----------------------------------------------------------------------------------------------------
echo.
if '%ELEVATED%'=='True' (
  %TC%[blockOpened name='Stopping Doppler Service']
  net stop DopplerInstrumentationService
  %TC%[blockClosed name='Stopping Doppler Service']
)
echo.
%TC%[blockOpened name='Delete Public\bin folder']
if exist %OUTDIR% (
  echo Deleting %OUTDIR%
  :: If running elevated force devenv to release handles
  if '%ELEVATED%'=='True' (
    echo Releasing handles held by devenv in %OUTDIR%
    for /f "skip=5 tokens=3,6 delims=: " %%i in ('handle.exe -accepteula -p devenv %OUTDIR%') do (
      %RUN% handle -accepteula -p %%i -c %%j -y >NUL
    )
  )
  :: Remove the directory
  %RUN% rmdir /q /s %OUTDIR%
)
%TC%[blockClosed name='Delete Public\bin folder']
echo.
%TC%[blockClosed name='Pre-build Clean']

:::::::::::::::::::::::::::::::::::
:: Build each solution
:::::::::::::::::::::::::::::::::::
:Build

set MSBUILDPROPS=TeamCity=%TEAM_CITY%
set MSBUILDPROPS=!MSBUILDPROPS!;PrivateConfig=%PRIVATECONFIG%
set MSBUILDPROPS=!MSBUILDPROPS!;TargetProfile=%TARGETPROFILE%
set MSBUILDPROPS=!MSBUILDPROPS!;Configuration=%CONFIGURATION%
set MSBUILDPROPS=!MSBUILDPROPS!;MockAuth=%MOCK_AUTH%
set MSBUILDPROPS=!MSBUILDPROPS!;DefaultUserId=%DEFAULT_USER_ID%
set MSBUILDPROPS=!MSBUILDPROPS!;RunNCover=%RUN_NCOVER%
set MSBUILDPROPS=!MSBUILDPROPS!;StyleCopTreatErrorsAsWarnings=%STYLECOP_AS_WARNINGS%
set MSBUILDPROPS=!MSBUILDPROPS!;FxCopAsWarnings=%FXCOP_AS_WARNINGS%
set MSBUILDPROPS=!MSBUILDPROPS!;SkipFxCop=%SKIP_FXCOP%
set MSBUILDPROPS=!MSBUILDPROPS!;GenerateXmlDocs=%XMLDOCS%
set MSBUILDPROPS=!MSBUILDPROPS!;RunTests=%RUN_UNIT_TESTS%
set MSBUILDPROPS=!MSBUILDPROPS!;ContinueOnTestFailure=True
set MSBUILDPROPS=!MSBUILDPROPS!;TestFilter=%TEST_FILTER%
set MSBUILDPROPS=!MSBUILDPROPS!;ExcludeTestCategory=%EXCLUDE_TEST_CATEGORY%

for /f "eol=#" %%i in (%SOLUTIONS_TO_BUILD%) do (
  %TC%[blockOpened name='Building %%i']
  echo.
  echo ----------------------------------------------------------------------------------------------------
  echo [!DATE! !TIME!] Building "%%i"
  echo ----------------------------------------------------------------------------------------------------
  echo.
  call :msbuild %%i
  set /a SOLUTIONS_BUILT=!SOLUTIONS_BUILT!+1
  echo.

  :: If successful and package option selected, build again for Publish target
  if !BUILD_RESULT!==0 if /i '%PACKAGE%'=='True' (

    set MSBUILDPROPS=TeamCity=%TEAM_CITY%
    set MSBUILDPROPS=!MSBUILDPROPS!;PrivateConfig=%PRIVATECONFIG%
    set MSBUILDPROPS=!MSBUILDPROPS!;TargetProfile=%TARGETPROFILE%
    set MSBUILDPROPS=!MSBUILDPROPS!;Configuration=%CONFIGURATION%
    set MSBUILDPROPS=!MSBUILDPROPS!;MockAuth=%MOCK_AUTH%
    set MSBUILDPROPS=!MSBUILDPROPS!;DefaultUserId=%DEFAULT_USER_ID%
    set MSBUILDPROPS=!MSBUILDPROPS!;FxCopAsWarnings=%FXCOP_AS_WARNINGS%
    set MSBUILDPROPS=!MSBUILDPROPS!;SkipFxCop=%SKIP_FXCOP%
    set MSBUILDPROPS=!MSBUILDPROPS!;GenerateXmlDocs=%XMLDOCS%

    for /f "eol=#" %%j in (%SOLUTIONS_TO_PACKAGE%) do if /i '%%i'=='%%j' (
      %TC%[blockOpened name='Packaging %%i']
      call :msbuild %%j Publish
      set /a SOLUTIONS_PACKAGED=!SOLUTIONS_PACKAGED!+1
      %TC%[blockClosed name='Packaging %%i']
    )
  )
  %TC%[blockClosed name='Building %%i']

  if !BUILD_RESULT! gtr 0 (
    echo ****************************************************************************************************
    echo [!DATE! !TIME!] BUILD FAILURE: %%i
    echo ****************************************************************************************************

    %TC%[message text='Failure Building Solution' errorDetails='Building of %%i failed' status='ERROR']
  
    if /i not '%CONTINUE_ON_BUILD_FAILURE%'=='TRUE' (
      goto Failed
    )
  ) else (
    set /a SOLUTIONS_SUCCEEDED=!SOLUTIONS_SUCCEEDED! + 1
  )
)

:::::::::::::::::::::::::::::::::::
:: Deploy Sql Database
:::::::::::::::::::::::::::::::::::
:InitStorage
if /i '%SQL_CLEAN%'=='True' (
	%TC%[blockOpened name='Clean Sql Databases']
	echo Drops tables from databases - may generate benign warnings if the database doesn't exist
	echo.
    for /f "eol=#" %%i in (%SQL_TO_INSTALL%) do (
        set SQL_INSTALL_CMD=%~dp0..\%%i\Setup\install.cmd
        echo !SQL_INSTALL_CMD! -s %SQL_SERVER% %SQL_USER% %SQL_PSWD% -delete
        call !SQL_INSTALL_CMD! -s %SQL_SERVER% %SQL_USER% %SQL_PSWD% -delete
    )
	echo.

	for %%p in (Local Integration) do if /i '%TARGETPROFILE%'=='%%p' (
    %TC%[blockOpened name='Clean Emulated Storage']
    echo %STOP_AZURE_STORE_COMMAND%
    %RUN% %STOP_AZURE_STORE_COMMAND%
    echo %CLEAN_AZURE_STORE_COMMAND%
    %RUN% %CLEAN_AZURE_STORE_COMMAND%
    echo %START_AZURE_STORE_COMMAND%
    %RUN% %START_AZURE_STORE_COMMAND%
    %TC%[blockClosed name='Clean Emulated Storage']
	)
  
  %TC%[blockClosed name='Clean Sql Databases']
)

if /i '%SQL_DEPLOY%'=='True' (
  %TC%[blockOpened name='Setup SQLExpress Mixed Mode AuthN and Networking']
  net stop MSSQL$SQLEXPRESS
  regedit.exe /s "%~dp0SQLExpressAuthN+Network.reg"
  net start MSSQL$SQLEXPRESS
  %TC%[blockClosed name='Setup SQLExpress Mixed Mode AuthN and Networking']
  
	%TC%[blockOpened name='Deploy Sql Databases']
	echo Creates database or updates - may generate benign warnings if the database exists already
	echo.
  for /f "eol=#" %%i in (%SQL_TO_INSTALL%) do (
    set SQL_INSTALL_CMD=%~dp0..\%%i\Setup\install.cmd
    echo !SQL_INSTALL_CMD! -s %SQL_SERVER% %SQL_USER% %SQL_PSWD% -create
    %RUN% call !SQL_INSTALL_CMD! -s %SQL_SERVER% %SQL_USER% %SQL_PSWD% -create
  )
  %TC%[blockClosed name='Deploy Sql Databases']
	echo.

	%TC%[blockOpened name='Initialize Default Entities']
    if /i '%TARGETPROFILE%'=='Cloud' (
      if '%SQL_CONNECTION_STRING%'=='' set SQL_CONNECTION_STRING=Data Source=%SQL_SERVER%;Initial Catalog=IndexDatastore;Integrated Security=False;User ID=lucyAppUser;Password=%settings.SQLUSERPWD%
    ) else if /i '%TARGETPROFILE%'=='ApnxAppSand' (
      if '%SQL_CONNECTION_STRING%'=='' set SQL_CONNECTION_STRING=Data Source=%SQL_SERVER%;Initial Catalog=IndexDatastore;Integrated Security=False;User ID=lucyAppUser;Password=%settings.SQLUSERPWD%
    )

    if /i '%CREATE_DEFAULT_COMPANY%'=='True' (
      set CREATE_COMPANY_EXE=%OUTDIR%\CreateCompany.exe
        echo !CREATE_COMPANY_EXE! -ics "!SQL_CONNECTION_STRING!" -eid %DEFAULT_COMPANY_ID% -cn %DEFAULT_COMPANY_NAME%
        %RUN% !CREATE_COMPANY_EXE! -ics "!SQL_CONNECTION_STRING!" -eid %DEFAULT_COMPANY_ID% -cn %DEFAULT_COMPANY_NAME%
        if !ERRORLEVEL! gtr 0 (
          %TC%[message text='Failure Creating Default Company' errorDetails='An error occurred while creating the default company.' status='ERROR']
          %TC%[blockClosed name='Initialize Default Entities']
          goto Failed
        )
    )
    
    if /i '%CREATE_DEFAULT_USER%'=='True' (
      set CREATE_USER_EXE=%OUTDIR%\CreateUser.exe
        echo !CREATE_USER_EXE! -ics "!SQL_CONNECTION_STRING!" -uid "%DEFAULT_USER_ID%" -acc "%DEFAULT_ACCESS%"
        %RUN% !CREATE_USER_EXE! -ics "!SQL_CONNECTION_STRING!" -uid "%DEFAULT_USER_ID%" -acc "%DEFAULT_ACCESS%"
        if !ERRORLEVEL! gtr 0 (
          %TC%[message text='Failure Creating Default User' errorDetails='An error occurred while creating the default user.' status='ERROR']
          %TC%[blockClosed name='Initialize Default Entities']
          goto Failed
        )
    )
  %TC%[blockClosed name='Initialize Default Entities']
)

if '%INIT_STORAGE_ONLY%'=='True' goto End

:::::::::::::::::::::::::::::::::::
:: Run Integration Tests
:::::::::::::::::::::::::::::::::::

if /i '%RUN_INTEGRATION_TESTS%'=='True' (
  %TC%[blockOpened name='Running integration tests']
    set MSBUILDPROPS=TeamCity=%TEAM_CITY%
    set MSBUILDPROPS=!MSBUILDPROPS!;PrivateConfig=%PRIVATECONFIG%
    set MSBUILDPROPS=!MSBUILDPROPS!;Configuration=%CONFIGURATION%
    set MSBUILDPROPS=!MSBUILDPROPS!;RunTests=True
    set MSBUILDPROPS=!MSBUILDPROPS!;TestFilter=IntegrationTests
    set MSBUILDPROPS=!MSBUILDPROPS!;ExcludeTestCategory=%EXCLUDE_TEST_CATEGORY%
    set MSBUILDPROPS=!MSBUILDPROPS!;RunNCover=False
    set MSBUILDPROPS=!MSBUILDPROPS!;StyleCopTreatErrorsAsWarnings=True
    set MSBUILDPROPS=!MSBUILDPROPS!;SkipFxCop=True
    set MSBUILDPROPS=!MSBUILDPROPS!;GenerateXmlDocs=%XMLDOCS%
    set MSBUILDPROPS=!MSBUILDPROPS!;ContinueOnTestFailure=True
    set MSBUILDPROPS=!MSBUILDPROPS!;MockAuth=%MOCK_AUTH%
    set MSBUILDPROPS=!MSBUILDPROPS!;DefaultUserId=%DEFAULT_USER_ID%  
    
    for /f "eol=#" %%i in (%INTEGRATION_TESTS%) do (
      %TC%[blockOpened name='Solution: %%i']
        call :msbuild %%i
        set /a INTEGRATION_TESTS_RUN=!INTEGRATION_TESTS_RUN!+1

        if !BUILD_RESULT! gtr 0 (
          echo ****************************************************************************************************
          echo [!DATE! !TIME!] POST-DEPLOYMENT TEST FAILURE: %%i
          echo ****************************************************************************************************
          %TC%[message text='Failure Running Integration Tests' errorDetails='Running of %%i failed' status='ERROR']
        ) else (
          set /a INTEGRATION_TESTS_PASSED=!INTEGRATION_TESTS_PASSED!+1
        )
      %TC%[blockClosed name='Solution: %%i']
    )
  %TC%[blockClosed name='Running integration tests']
  
  set /a INTEGRATION_TESTS_FAILED=!INTEGRATION_TESTS_RUN!-!INTEGRATION_TESTS_PASSED!
  if !INTEGRATION_TESTS_FAILED! gtr 0 (
    %TC%[message text='Integration Test Failure' errorDetails='!INTEGRATION_TESTS_FAILED! solutions contained failing integration tests' status='ERROR']
  )
)
  
:::::::::::::::::::::::::::::::::::
:: Deploy Azure Packages
:::::::::::::::::::::::::::::::::::
if /i '%DEPLOY%'=='True' if /i '%TARGETPROFILE%'=='production' (
  for %%i in (CIA1 CIA2 CIA3) do (
    if /i '%COMPUTERNAME%'=='%%i' goto Deploy
  )
  %TC%[message text='Unauthorized' errorDetails='This machine, %COMPUTERNAME%, is not authorized to do production deployments.' status='ERROR']
  goto Failed
)

:Deploy
if /i '%DEPLOY%'=='True' (
  set POWERSHELL=powershell -ExecutionPolicy unrestricted -f
  if not '!RUN!'=='' set POWERSHELL=echo # !POWERSHELL!
  set AZURE_DEPLOY_PS1=%~dp0AzureDeploy\AzureDeploy.ps1
  set AZURE_WAIT_PS1=%~dp0AzureDeploy\AzureWaitForDeployment.ps1
  set AZURE_SWAP_PS1=%~dp0AzureDeploy\AzureSwap.ps1
  set AZURE_DELETE_PS1=%~dp0AzureDeploy\AzureDeleteDeployment.ps1
  set AZURE_DEPLOYMENTID_PS1=%~dp0AzureDeploy\GetDeploymentId.ps1
  
  :: Deploy the cloud project packages
  %TC%[blockOpened name='Deploying to Windows Azure']
  set MESSAGE=Deploying !DEPLOYMENTS! hosted services to %DEPLOYMENT_SLOT%...
  %TC%[message text='!MESSAGE!']
  echo.
  echo ----------------------------------------------------------------------------------------------------
  echo [!DATE! !TIME!] !MESSAGE!
  echo ----------------------------------------------------------------------------------------------------
  echo.
  for /f "eol=# tokens=1,2,3 delims=;" %%i in (%PROJECTS_TO_DEPLOY%) do (
    %TC%[blockOpened name='Deploying %%j']
    echo Deploying %%j using profile %%k from %%i
    set PROJECT_PATH=Azure\%%i
    set PUBLISH_PATH=!PROJECT_PATH!\bin\%CONFIGURATION%\app.publish
    set PACKAGE_NAME=%%j
    set PUBLISH_PROFILE=%AZUREPROFILES%\%%i\%%k
    echo !POWERSHELL! !AZURE_DEPLOY_PS1! !PUBLISH_PATH! !PACKAGE_NAME! !PUBLISH_PROFILE! !AZURECONNECTIONS! staging
    !POWERSHELL! !AZURE_DEPLOY_PS1! !PUBLISH_PATH! !PACKAGE_NAME! !PUBLISH_PROFILE! !AZURECONNECTIONS! staging
    if !ERRORLEVEL! gtr 0 (
      :: Ignore 888 until AzureDeploy.ps1 fixed to not generate errors from checking for non-existent deployments
      if !ERRORLEVEL! neq 888 %TC%[message text='Failure Deploying Package' errorDetails='Deployment of %%j failed with error !ERRORLEVEL!' status='ERROR']
    ) else (
      set /a DEPLOYMENTS_DEPLOYED=!DEPLOYMENTS_DEPLOYED!+1
    )
    %TC%[blockClosed name='Deploying %%j']
  )
  :: Wait for deployment to complete
  %TC%[blockOpened name='Waiting for deployments to complete']
    echo Waiting for deployments to complete...
    for /f "eol=# tokens=1,2,3 delims=;" %%i in (%PROJECTS_TO_DEPLOY%) do (
      set PROJECT_PATH=Azure\%%i
      set PUBLISH_PROFILE=%AZUREPROFILES%\%%i\%%k
      !POWERSHELL! !AZURE_WAIT_PS1! !PUBLISH_PROFILE! !AZURECONNECTIONS! Ready 900 staging
      echo.
    )
  %TC%[blockClosed name='Waiting for deployments to complete']

  %TC%[blockClosed name='Deploying to Windows Azure']
  
  :: Swap deployments from staging to production
  %TC%[blockOpened name='Swapping deployments from staging to production']
    echo Swapping deployments from staging to production...
    for /f "eol=# tokens=1,2,3 delims=;" %%i in (%PROJECTS_TO_DEPLOY%) do (
      set PROJECT_PATH=Azure\%%i
      set PUBLISH_PROFILE=%AZUREPROFILES%\%%i\%%k
      !POWERSHELL! !AZURE_SWAP_PS1! !PUBLISH_PROFILE! !AZURECONNECTIONS!
    )
  %TC%[blockClosed name='Swapping deployments from staging to production']

  :: Get the deployment IDs
  %TC%[blockOpened name='Getting deployment IDs']
    set PRODUCTION_DEPLOYMENTID=
    for /f "tokens=1,2 delims= " %%d in ('!POWERSHELL! !AZURE_DEPLOYMENTID_PS1! !PUBLISH_PROFILE! !AZURECONNECTIONS! production ^| find "DeploymentId: "') do set PRODUCTION_DEPLOYMENTID=%%e
    echo Production Deployment: "!PRODUCTION_DEPLOYMENTID!"
    set STAGING_DEPLOYMENTID=
    for /f "tokens=1,2 delims= " %%d in ('!POWERSHELL! !AZURE_DEPLOYMENTID_PS1! !PUBLISH_PROFILE! !AZURECONNECTIONS! staging ^| find "DeploymentId: "') do set STAGING_DEPLOYMENTID=%%e
    echo Staging Deployment: "!STAGING_DEPLOYMENTID!"
  %TC%[blockClosed name='Getting deployment IDs']

  if not '!STAGING_DEPLOYMENTID!'=='' (
    :: Delete old deployment which is now in the staging slot
    %TC%[blockOpened name='Shutting down previous deployment: "!STAGING_DEPLOYMENTID!"']

    :: Land the staging worker role before deleting it
    if /i '%LAND_WORKER_ROLE%'=='True' (
      :: Set production deployment state to "Launched" and make active
      %TC%[blockOpened name='Setting production deployment !PRODUCTION_DEPLOYMENTID! state to Launched']
        %RUN% DProps -d "!PRODUCTION_DEPLOYMENTID!" -c set -pn State -pv Launched
        %RUN% DProps -d "!PRODUCTION_DEPLOYMENTID!" -c get -pn State
        %RUN% DProps -d "[ACTIVE]" -c set -pn DeploymentId -pv "!PRODUCTION_DEPLOYMENTID!"
        %RUN% DProps -d "[ACTIVE]" -c get -pn DeploymentId
      %TC%[blockClosed name='Setting production deployment !PRODUCTION_DEPLOYMENTID! state to Launched']
    
      :: Set staging deployment state to "Landing"
      %TC%[blockOpened name='Setting staging deployment !STAGING_DEPLOYMENTID! state to Landing']
        %RUN% DProps -d "!STAGING_DEPLOYMENTID!" -c set -pn State -pv Landing
        %RUN% DProps -d "!STAGING_DEPLOYMENTID!" -c get -pn State 
      %TC%[blockClosed name='Setting staging deployment !STAGING_DEPLOYMENTID! state to Landing']

      :: Wait for staging deployment roles to land
      %TC%[blockOpened name='Waiting for staging deployment !STAGING_DEPLOYMENTID! role instances to land']
        echo Role states:
        %RUN% DProps -d "!STAGING_DEPLOYMENTID!" -c get -r -pn RoleState
        
        :: Maximum wait is 10 minutes, 60 x 10 seconds + check time
        set ROLE_LANDING_WAITS=60
        
        :RoleLandingWait
        echo Waiting for roles to land... !ROLE_LANDING_WAITS!
        choice /t 10 /d y >NUL
        
        :: Check if all the roles have landed
        set ALL_ROLES_LANDED=True
        for /f "tokens=1,2 delims==" %%i in ('dprops -d "!STAGING_DEPLOYMENTID!" -c get -r -pn RoleState') do (
          if /i not '%%j'=='Landed' set ALL_ROLES_LANDED=False
        )
        if '!ALL_ROLES_LANDED!'=='False' (
          set /a ROLE_LANDING_WAITS=!ROLE_LANDING_WAITS! - 1
          if !ROLE_LANDING_WAITS!==0 (
            %TC%[message text='Failure Landing Role Instances' errorDetails='Landing of role instances failed to complete within the time allowed.' status='ERROR']
          ) else (
            goto RoleLandingWait
          )
        ) else (
          %RUN% DProps -d "!STAGING_DEPLOYMENTID!" -c set -pn State -pv Landed
        )
        echo Role states:
        %RUN% DProps -d "!STAGING_DEPLOYMENTID!" -c get -r -pn RoleState
      %TC%[blockClosed name='Waiting for staging deployment !STAGING_DEPLOYMENTID! role instances to land']
    
      :: Delete work-item queues from deleted deployment
      %TC%[blockOpened name='Deleting work-item queues from previous deployment']
      echo Deleting queues from previous deployment: "!STAGING_DEPLOYMENTID!"
      %RUN% DelQueues -f -i "!STAGING_DEPLOYMENTID!" -x "!PRODUCTION_DEPLOYMENTID!"
      %TC%[blockClosed name='Deleting work-item queues from previous deployment']
    )
    
    %TC%[blockOpened name='Deleting staging deployment !STAGING_DEPLOYMENTID!']
    for /f "eol=# tokens=1,2,3 delims=;" %%i in (%PROJECTS_TO_DEPLOY%) do (
      set PROJECT_PATH=Azure\%%i
      set PUBLISH_PROFILE=%AZUREPROFILES%\%%i\%%k
      !POWERSHELL! !AZURE_DELETE_PS1! !PUBLISH_PROFILE! !AZURECONNECTIONS! staging
      if !ERRORLEVEL! gtr 0 (
        %TC%[message text='Failure Deleting Deployment' errorDetails='Deleting of staging deployment of %%i failed with error !ERRORLEVEL!' status='ERROR']
        goto Failed
      )
    )
    %TC%[blockClosed name='Deleting staging deployment !STAGING_DEPLOYMENTID!']

    %TC%[blockClosed name='Shutting down previous deployment: "!STAGING_DEPLOYMENTID!"']
  )
)
  
:: Run Post-Deployment Tests
if /i '%RUN_POST_DEPLOYMENT_TESTS%'=='True' (
  %TC%[blockOpened name='Running post-deployment tests']
    set MSBUILDPROPS=TeamCity=%TEAM_CITY%
    set MSBUILDPROPS=!MSBUILDPROPS!;PrivateConfig=%PRIVATECONFIG%
    set MSBUILDPROPS=!MSBUILDPROPS!;Configuration=%CONFIGURATION%
    set MSBUILDPROPS=!MSBUILDPROPS!;RunTests=True
    set MSBUILDPROPS=!MSBUILDPROPS!;TestFilter=%TEST_FILTER%
    set MSBUILDPROPS=!MSBUILDPROPS!;ExcludeTestCategory=%EXCLUDE_TEST_CATEGORY%
    set MSBUILDPROPS=!MSBUILDPROPS!;RunNCover=False
    set MSBUILDPROPS=!MSBUILDPROPS!;StyleCopTreatErrorsAsWarnings=True
    set MSBUILDPROPS=!MSBUILDPROPS!;SkipFxCop=True
    set MSBUILDPROPS=!MSBUILDPROPS!;GenerateXmlDocs=%XMLDOCS%
    set MSBUILDPROPS=!MSBUILDPROPS!;ContinueOnTestFailure=True
    
    set MESSAGE=Running post-deployment tests from "%POST_DEPLOYMENT_TESTS%"...
    %TC%[message text='!MESSAGE!']
    echo ----------------------------------------------------------------------------------------------------
    echo [!DATE! !TIME!] !MESSAGE!
    echo ----------------------------------------------------------------------------------------------------
    
    for /f "eol=#" %%i in (%POST_DEPLOYMENT_TESTS%) do (
      %TC%[blockOpened name='Running post-deployment test: %%i']
        echo Running post-deployment test: %%i...
        call :msbuild %%i
        set /a DEPLOYMENT_TESTS_RUN=!DEPLOYMENT_TESTS_RUN!+1

        if !BUILD_RESULT! gtr 0 (
          echo ****************************************************************************************************
          echo [!DATE! !TIME!] POST-DEPLOYMENT TEST FAILURE: %%i
          echo ****************************************************************************************************
          %TC%[message text='Failure Running Post-Deployment Tests' errorDetails='Running of %%i failed' status='ERROR']
        ) else (
          set /a DEPLOYMENT_TESTS_PASSED=!DEPLOYMENT_TESTS_PASSED!+1
        )
      %TC%[blockClosed name='Running post-deployment test: %%i']
    )
  %TC%[blockClosed name='Running post-deployment tests']
  
  set /a DEPLOYMENT_TESTS_FAILED=!DEPLOYMENT_TESTS_RUN!-!DEPLOYMENT_TESTS_PASSED!
  if !DEPLOYMENT_TESTS_FAILED! gtr 0 (
    %TC%[message text='Post-Deployment Test Failed' errorDetails='!DEPLOYMENT_TESTS_FAILED! post-deployment solutions contained failing tests' status='ERROR']
  )
)

:: Delete Deployments
if /i '%DEPLOY%'=='True' if /i '%DELETE_DEPLOYMENT%'=='True' (
  %TC%[blockOpened name='Deleting deployments']
    set MESSAGE=Deleting !DEPLOYMENTS! deployments from production...
    %TC%[message text='!MESSAGE!']
    echo ----------------------------------------------------------------------------------------------------
    echo [!DATE! !TIME!] !MESSAGE!
    echo ----------------------------------------------------------------------------------------------------
    
    for /f "eol=# tokens=1,2,3 delims=;" %%i in (%PROJECTS_TO_DEPLOY%) do (
      echo Deleting deployment of %%j
      set PROJECT_PATH=Azure\%%i
      set PUBLISH_PROFILE=%AZUREPROFILES%\%%i\%%k
      !POWERSHELL! !AZURE_DELETE_PS1! !PUBLISH_PROFILE! !AZURECONNECTIONS! production
    )
  %TC%[blockClosed name='Deleting deployments']
)

goto End

:Failed
:: Need to set BUILD_RESULT because it could be reset to 0 if CONTINUE_ON_BUILD_FAILURE is enabled
set BUILD_RESULT=1

:End
echo.
set END_TIME=%TIME%
for /f "tokens=1-3 delims=:." %%i in ("%START_TIME%") do set /a START_TIME_SECONDS=%%i * 3600 + %%j * 60 + %%k
for /f "tokens=1-3 delims=:." %%i in ("%END_TIME%") do set /a END_TIME_SECONDS=%%i * 3600 + %%j * 60 + %%k
set /a BUILD_TIME=%END_TIME_SECONDS%-%START_TIME_SECONDS%
%TC%[blockOpened name='ResultSummary']
  if %BUILD_RESULT% gtr 0 (
    set RESULT=Failed
  ) else (
    set RESULT=Succeeded
  )
  set RESULT_MESSAGE=Build %RESULT%: %SOLUTIONS_SUCCEEDED% of %SOLUTIONS% Solutions Successful (%SOLUTIONS_BUILT% Attempted)
  echo ====================================================================================================
  echo [%DATE% %TIME%] Build %RESULT%
  echo ====================================================================================================
  echo %SOLUTIONS_SUCCEEDED% / %SOLUTIONS% Successful (%SOLUTIONS_BUILT% Attempted)
  echo %SOLUTIONS_PACKAGED% Cloud Packages Created.
  echo %DEPLOYMENTS_DEPLOYED% / %DEPLOYMENTS% Hosted Services Deployed.
  echo Started: %START_TIME% Finished: %END_TIME% Duration: %BUILD_TIME%s
%TC%[blockClosed name='ResultSummary']
popd

::
:: NOTE: Exit must be in the same line as endlocal to pass %BUILD_RESULT% out of the local context
::
endlocal & exit /b %BUILD_RESULT%

:Usage
echo %~nx0 [Debug^|Release] [NoFxCop] [NCover] [RunAllTests^|RunIntegrationTests^|RunUnitTests^|RunBVTs]
goto :EOF

::::::::::::::::::::::::::::::
:: Subroutines
::::::::::::::::::::::::::::::

::----------------------------
:msbuild
::----------------------------
:: Check if the solution is okay to build in parallel or not
set msbuild_sln=%1
if not "%2"=="" (
  set msbuild_target=%2
) else (
  set msbuild_target=%TARGETS%
)

set msbuild_exe=msbuild /m
for /f "eol=#" %%i in (%SERIAL_BUILD_SOLUTIONS%) do if /i '%msbuild_sln%'=='%%i' (
    set msbuild_exe=msbuild /m:1
    echo Not building %1 in parallel.
)

echo Running build: %RUN% %msbuild_exe% "%msbuild_sln%" /target:%msbuild_target% /property:!MSBUILDPROPS!
%RUN% %msbuild_exe% "%msbuild_sln%" /target:%msbuild_target% /property:!MSBUILDPROPS!
set BUILD_RESULT=!ERRORLEVEL!
echo MSBuild for %msbuild_sln% exited with result: %BUILD_RESULT%
goto :EOF
::----------------------------

