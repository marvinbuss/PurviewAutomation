# Application Settings

Application Settings of the Azure function can be used to configure some of the features of this solution. The application settings as well as their effect will be described below:

| Application Setting Name | Default Value | Description |
|:-------------------------|:--------------|:------------|
| UseManagedPrivateEndpoints | `True`      | This parameter allows you to specify that managed private endpoints should be created automatically for Azure data sources. |
| ManagedIntegrationRuntimeName | `defaultIntegrationRuntime` | Name of the managed integration runtime that will be used for scanning Azure data sources. Will only be effective if `UseManagedPrivateEndpoints` is set to `True`. |
| SetupScan                | `True`        | This parameter allows you to turn the setup of scans on or off. Set this value to `False` to turn the automated setup of scans off. |
| TriggerScan              | `True`        | This parameter allows you to turn the trigger of the initial scan on or off. This parameter only has an effect if `SetupScan` is set to `True`. Set this value to `False` to turn the initial trigger off. |
| SetupLineage             | `True`        | This parameter allows you to turn the setup of lineage on or off. Set this value to `False` to turn the automated setup of lineage off. |
| RemoveDataSources        | `True`        | This parameter allows you to turn the removal of data sources on or off. Set this value to `False` to turn the automated removal of data sources off. |

Leave the other parameters to the default to assure that the solution works as expected.
