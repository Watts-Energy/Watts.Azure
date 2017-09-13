# Information
In order to execute Integration Tests and Manual Tests in this project you will need to fill in various account information related to batch.

When you run the first test, a file will be generated in the root directory of the test project, named **testEnvironment.testenv**.

This file is ignored by git (the pattern *.testenv) and will not be commited. Don't change that or rename the file, as that could mean
you'd be uploading your Azure credentials to github. 
We will of course review any pull requests and make sure that noone does this by accident, but it's better to be completely sure.

Add your relevant credentials to the file (which contains a json object deserialized into TestEnvironmentConfig.cs when each Integration/Manual test starts).

In case you only want to run Integration/Manual tests relevant to Batch, you will only need to fill in the settings:
- BatchAccountName
- BatchAccountKey
- BatchAccountUrl
- StorageAccountName
- StorageAccountKey

To run Data Factory (Copy data) tests, fill in 
- SubscriptionId
- ActiveDirectoryTenantId
- AdfClientId
- ClientSecret
- StorageAccountName
- StorageAccountKey

for the Data Factory account/app registration as well as all the same settings for the DataL

The details of how to obtain the keys/secrets etc. are explained in the main project README.

In addition to the above there's a single test of using Azure File Share to upload/download data. To execute those you need to fill in 
- FileshareConnectionString