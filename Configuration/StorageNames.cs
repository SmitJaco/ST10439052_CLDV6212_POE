using System;

namespace ST10439052_CLDV_POE.Configuration
{
    public static class StorageNames
    {
        // Blob containers
        public const string ContainerUploads = "uploads";
        public const string ContainerProductImages = "product-images";
        public const string ContainerPaymentProofs = "payment-proofs";

        // Queues
        public const string QueueOrders = "orders-queue";
        public const string QueueOrderNotifications = "order-notifications";
        public const string QueueStockUpdates = "stock-updates";
        public const string QueueOrderNotificationsPoison = "order-notifications-poison";
        public const string QueueStockUpdatesPoison = "stock-updates-poison";

        // File shares
        public const string ShareContracts = "contracts";
        public const string ShareContractsPaymentsDir = "payments";
    }
}

