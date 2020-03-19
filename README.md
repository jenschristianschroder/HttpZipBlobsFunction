# HttpZipBlobsFunction
An Azure Function to zip files in Azure Storage Container

# Usage
```
curl -H "Content-Type: application/json" -X POST https://function.url -d "[\"filename1.ext\", \"filename2.ext\"]"
```

# Returns 
```
https://storageaccount/container/zipfile.zip
```

# Required settings
```
{
  "Values": {
    "sourceStorageConnectionString": "[Yout Storage Account Connection String]",
    "sourceContainer": "[Source Container]",
    "destinationStorageConnectionString": "[Your Storage Account Connection String]",
    "destinationContainer": "[Destination Container]"
  }
}
```
