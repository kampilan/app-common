using AppCommon.Core.Mediator;
using Shouldly;
using Xunit;

namespace AppCommon.Core.Tests.Mediator;

public class BatchExecutionContextTests
{
    [Fact]
    public void IsInBatch_WhenNotInBatch_ReturnsFalse()
    {
        // Assert
        BatchExecutionContext.IsInBatch.ShouldBeFalse();
    }

    [Fact]
    public void IsInBatch_WhenInBatch_ReturnsTrue()
    {
        // Act
        using (BatchExecutionContext.BeginBatch("test-batch"))
        {
            // Assert
            BatchExecutionContext.IsInBatch.ShouldBeTrue();
        }
    }

    [Fact]
    public void IsInBatch_AfterBatchDisposed_ReturnsFalse()
    {
        // Act
        using (BatchExecutionContext.BeginBatch("test-batch"))
        {
            // Inside batch
        }

        // Assert - after dispose
        BatchExecutionContext.IsInBatch.ShouldBeFalse();
    }

    [Fact]
    public void BatchId_WhenNotInBatch_ReturnsNull()
    {
        // Assert
        BatchExecutionContext.BatchId.ShouldBeNull();
    }

    [Fact]
    public void BatchId_WhenInBatch_ReturnsBatchId()
    {
        // Arrange
        const string expectedBatchId = "my-batch-123";

        // Act
        using (BatchExecutionContext.BeginBatch(expectedBatchId))
        {
            // Assert
            BatchExecutionContext.BatchId.ShouldBe(expectedBatchId);
        }
    }

    [Fact]
    public void Depth_WhenNotInBatch_ReturnsZero()
    {
        // Assert
        BatchExecutionContext.Depth.ShouldBe(0);
    }

    [Fact]
    public void Depth_WhenInSingleBatch_ReturnsOne()
    {
        // Act
        using (BatchExecutionContext.BeginBatch("batch-1"))
        {
            // Assert
            BatchExecutionContext.Depth.ShouldBe(1);
        }
    }

    [Fact]
    public void Depth_WhenNestedBatches_IncrementsCorrectly()
    {
        // Act & Assert
        BatchExecutionContext.Depth.ShouldBe(0);

        using (BatchExecutionContext.BeginBatch("outer"))
        {
            BatchExecutionContext.Depth.ShouldBe(1);

            using (BatchExecutionContext.BeginBatch("inner"))
            {
                BatchExecutionContext.Depth.ShouldBe(2);

                using (BatchExecutionContext.BeginBatch("innermost"))
                {
                    BatchExecutionContext.Depth.ShouldBe(3);
                }

                BatchExecutionContext.Depth.ShouldBe(2);
            }

            BatchExecutionContext.Depth.ShouldBe(1);
        }

        BatchExecutionContext.Depth.ShouldBe(0);
    }

    [Fact]
    public void BeginBatch_WithNullBatchId_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => BatchExecutionContext.BeginBatch(null!));
    }

    [Fact]
    public void BeginBatch_WithEmptyBatchId_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => BatchExecutionContext.BeginBatch(""));
    }

    [Fact]
    public void BeginBatch_WithWhitespaceBatchId_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => BatchExecutionContext.BeginBatch("   "));
    }

    [Fact]
    public void BatchId_InNestedBatch_ReturnsInnermostBatchId()
    {
        // Act & Assert
        using (BatchExecutionContext.BeginBatch("outer-batch"))
        {
            BatchExecutionContext.BatchId.ShouldBe("outer-batch");

            using (BatchExecutionContext.BeginBatch("inner-batch"))
            {
                BatchExecutionContext.BatchId.ShouldBe("inner-batch");
            }

            BatchExecutionContext.BatchId.ShouldBe("outer-batch");
        }
    }

    [Fact]
    public async Task BatchContext_IsIsolatedPerAsyncFlow()
    {
        // Arrange
        var task1BatchId = "";
        var task2BatchId = "";

        // Act
        var task1 = Task.Run(async () =>
        {
            using (BatchExecutionContext.BeginBatch("task1-batch"))
            {
                await Task.Delay(50);
                task1BatchId = BatchExecutionContext.BatchId!;
            }
        });

        var task2 = Task.Run(async () =>
        {
            using (BatchExecutionContext.BeginBatch("task2-batch"))
            {
                await Task.Delay(50);
                task2BatchId = BatchExecutionContext.BatchId!;
            }
        });

        await Task.WhenAll(task1, task2);

        // Assert - each task should have its own batch context
        task1BatchId.ShouldBe("task1-batch");
        task2BatchId.ShouldBe("task2-batch");
    }
}
