## Database Descriptions
This is a document that provides information about each of the database snapshots
in this test project. Please keep this stable sorted by date.

## Adding new test data
To add new data, set up the app in the state you want to capture. Next log out of
the app. This is critical as it will clear out your personal data from the database (via excise).

Next zip up the `MnemonicDB.rocksdb` folder from the app folder, and place it in the `Resources/Databases` folder with a name
that includes the date of the snapshot. Note, the tests expect there to be a `MnemonicDB.rocksdb` folder in the zip file at the root.
So don't zip up the contents of the folder, zip up the folder itself.

Finally, update the `Descriptions.md` file with the new data.

## Descriptions

| Date             | Database Name                | Description                                                                                  |
|------------------|----------------------------|----------------------------------------------------------------------------------------------|
| 2024-11-04 | `SDV.4_11_2024.rocksdb.zip`  | A snapshot of SDV managed and with the "Stardew Valley VERY Expanded" collection installed.  |
