CREATE PROCEDURE dbo.usp_PlaceBid
    @ItemId INT,
    @BidderId INT,
    @Amount DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @now DATETIME2 = SYSUTCDATETIME();
    BEGIN TRY
        BEGIN TRAN;

        SELECT ItemId, MinBid, BidIncrement, StartDate, EndDate, Status
        INTO #item
        FROM dbo.Items WITH (UPDLOCK, HOLDLOCK)
        WHERE ItemId = @ItemId;

        IF NOT EXISTS (SELECT 1 FROM #item)
        BEGIN
            RAISERROR('Item not found',16,1);
            ROLLBACK TRAN; RETURN;
        END

        IF (SELECT Status FROM #item) <> 'A'
        BEGIN
            RAISERROR('Auction is not active',16,1);
            ROLLBACK TRAN; RETURN;
        END

        IF @now < (SELECT StartDate FROM #item) OR @now > (SELECT EndDate FROM #item)
        BEGIN
            RAISERROR('Auction is not accepting bids (outside time window)',16,1);
            ROLLBACK TRAN; RETURN;
        END

        DECLARE @currentMax DECIMAL(18,2);
        SELECT @currentMax = MAX(Amount) FROM dbo.Bids WHERE ItemId = @ItemId;

        IF @currentMax IS NULL
            SET @currentMax = (SELECT MinBid FROM #item) - (SELECT BidIncrement FROM #item);

        DECLARE @requiredMin DECIMAL(18,2) = @currentMax + (SELECT BidIncrement FROM #item);

        IF @Amount < @requiredMin
        BEGIN
            RAISERROR('Bid amount too low. Minimum required is %.2f',16,1,@requiredMin);
            ROLLBACK TRAN; RETURN;
        END

       
        INSERT INTO dbo.Bids (ItemId, BidderId, Amount)
        VALUES (@ItemId, @BidderId, @Amount);

        DECLARE @newBidId INT = SCOPE_IDENTITY();

        
        UPDATE dbo.Bids SET IsWinning = 0 WHERE ItemId = @ItemId AND BidId <> @newBidId;

        
        UPDATE dbo.Bids SET IsWinning = 1 WHERE BidId = @newBidId;

      
        UPDATE dbo.Items
        SET CurrentPrice = @Amount,
            CurrentBidId = @newBidId
        WHERE ItemId = @ItemId;

      

        COMMIT TRAN;
        SELECT 'OK' AS Result, @newBidId AS NewBidId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        DECLARE @ErrMsg NVARCHAR(4000)=ERROR_MESSAGE(), @ErrNo INT = ERROR_NUMBER();
        RAISERROR('Error placing bid: %s',16,1,@ErrMsg);
    END CATCH
END
GO
