using Azure;
using Azure.Data.Tables;
using Service.Azure.Storage.Table;
using Xunit.Abstractions;

namespace Tests.Storage.Table
{
    public class TableStorageIntegrationTests(ITableStorageService tableStorageService, ITestOutputHelper output)
    {
        private const string TableName = "TestTable"; // Only test tables containing 'xunit' can be deleted
        private readonly string PartitionKey = "TestPartition";
        private readonly string RowKey = "TestRow";

        public class TestEntity : ITableEntity
        {
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public string Name { get; set; } = "InitialName";
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; } = ETag.All; // Provide a default ETag value

            public TestEntity()
            {
                PartitionKey = "TestPartition";
                RowKey = "TestRow";
            }
        }

        [Fact]
        public async Task TableStorage_CRUD_Works()
        {
            // Initialize table client and ensure table exists
            tableStorageService.InitializeTableClient(TableName, checkTable: true);

            // CREATE
            var entity = new TestEntity();
            var addResult = await tableStorageService.AddRecord(TableName, entity);
            output.WriteLine($"AddRecord result: {addResult}");
            Assert.True(addResult);

            // READ
            var readEntity = await tableStorageService.GetRecord<TestEntity>(TableName, PartitionKey, RowKey);
            output.WriteLine($"Read entity name: {readEntity?.Name}");
            Assert.NotNull(readEntity);
            Assert.Equal(entity.Name, readEntity!.Name);

            // UPDATE
            var updates = new Dictionary<string, object?> { { "Name", "UpdatedName" } };
            var updateResult = await tableStorageService.UpdateRecord(TableName, PartitionKey, RowKey, updates);
            output.WriteLine($"UpdateRecord result: {updateResult}");
            Assert.True(updateResult);

            // READ after update
            var updatedEntity = await tableStorageService.GetRecord<TestEntity>(TableName, PartitionKey, RowKey);
            output.WriteLine($"Updated entity name: {updatedEntity?.Name}");
            Assert.NotNull(updatedEntity);
            Assert.Equal("UpdatedName", updatedEntity!.Name);

            // DELETE
            var deleteResult = await tableStorageService.DeleteRecord(TableName, PartitionKey, RowKey);
            output.WriteLine($"DeleteRecord result: {deleteResult}");
            Assert.True(deleteResult);

            // READ after delete
            var deletedEntity = await tableStorageService.GetRecord<TestEntity>(TableName, PartitionKey, RowKey);
            output.WriteLine($"Entity after delete: {deletedEntity}");
            Assert.Null(deletedEntity);

            // Clean up: remove the test table
            var removeTableResult = await tableStorageService.RemoveTable(TableName);
            output.WriteLine($"RemoveTable result: {removeTableResult}");
            Assert.True(removeTableResult);
        }
    }
}
