# ABC Retailers - Retail Management System

## Overview
ABC Retailers is a comprehensive retail management system built with ASP.NET Core MVC that provides complete inventory, customer, and order management capabilities using Azure Storage services.

## Features

### 🏪 **Customer Management**
- Add, edit, and delete customer records
- Store customer contact information and addresses
- View customer order history and statistics
- Search and filter customer data

### 📦 **Product Management**
- Comprehensive product catalog with categories and brands
- Stock quantity tracking with low stock alerts
- Product image upload and management
- Price and description management

### 🛒 **Order Management**
- Create and manage customer orders
- Track order status (Submitted, Processing, Shipped, Delivered, Cancelled)
- Order item management with quantity and pricing
- Order history and analytics
- Queue-based notifications for order updates

### 📁 **File Upload System**
- Secure file storage and management using Azure Blob Storage
- Support for multiple file types (images, documents, archives)
- File metadata tracking (size, type, upload date)
- Download and preview capabilities

### 📊 **Dashboard & Analytics**
- Real-time business metrics
- Revenue tracking and reporting
- Customer and product statistics
- Order status overview

## Technology Stack

- **Framework**: ASP.NET Core 9.0 MVC
- **Storage**: Azure Storage (Tables, Blobs, Queues)
- **Frontend**: Bootstrap 5, jQuery, Bootstrap Icons
- **Architecture**: Service-based architecture with dependency injection

## Project Structure

```
ST10439052_CLDV_POE/
├── Controllers/           # MVC Controllers
│   ├── HomeController.cs  # Dashboard and main navigation
│   ├── CustomerController.cs # Customer CRUD operations
│   ├── ProductController.cs  # Product CRUD operations
│   ├── OrderController.cs    # Order CRUD operations
│   └── UploadController.cs   # File upload management
├── Models/               # Data Models
│   ├── Customer.cs      # Customer entity
│   ├── Product.cs       # Product entity
│   ├── Order.cs         # Order entity
│   └── FileUploadModel.cs # File upload model
├── Models/ViewModels/   # View Models
│   ├── HomeViewModel.cs # Dashboard view model
│   └── OrderCreateViewModel.cs # Order creation view model
├── Services/            # Business Logic Services
│   ├── IAzureStorageService.cs # Azure Storage interface
│   └── MockAzureStorageService.cs # Mock implementation for development
├── Views/               # Razor Views
│   ├── Home/           # Dashboard views
│   ├── Customer/       # Customer management views
│   ├── Product/        # Product management views
│   ├── Order/          # Order management views
│   ├── Upload/         # File upload views
│   └── Shared/         # Layout and shared components
└── Program.cs          # Application configuration
```

## Data Storage Schema

### Azure Table Storage

#### Customers Table
- PartitionKey: "Customer"
- RowKey: CustomerId (GUID)
- Properties: FirstName, LastName, Email, Phone, Address, City, PostalCode, Country

#### Products Table
- PartitionKey: "Product"
- RowKey: ProductId (GUID)
- Properties: ProductName, Description, Price, StockAvailable, Category, Brand, ImageUrl

#### Orders Table
- PartitionKey: "Order"
- RowKey: OrderId (GUID)
- Properties: CustomerId, ProductId, OrderDate, Quantity, UnitPrice, TotalPrice, Status, Notes

### Azure Blob Storage
- **product-images**: Product images and media files
- **payment-proofs**: Payment verification documents
- **contracts**: Business contracts and agreements

### Azure Queue Storage
- **order-notifications**: Order status updates and notifications
- **stock-updates**: Inventory level changes and alerts

## Setup Instructions

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- Azure Storage Account (for production)

### Installation Steps

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd ST10439052_CLDV_POE
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

4. **Access the application**
   - Navigate to `https://localhost:5001` or `http://localhost:5000`
   - The dashboard will be displayed with navigation to all modules

### Development Mode
The application uses a mock Azure Storage service for development, which provides:
- In-memory data storage
- Sample data initialization
- Simulated async operations
- No external dependencies

### Production Setup
To use real Azure Storage services:
1. Create an Azure Storage Account
2. Update the service implementation to use Azure SDK
3. Configure connection strings in `appsettings.json`
4. Replace `MockAzureStorageService` with real implementation

## Usage Guide

### Getting Started
1. **Dashboard**: View business overview and quick statistics
2. **Customers**: Add your first customer to start taking orders
3. **Products**: Create your product catalog with categories and pricing
4. **Orders**: Create orders by selecting customers and products
5. **File Uploads**: Upload product images and business documents

### Customer Management
- Use the Customer module to maintain customer database
- Each customer can have multiple orders
- View customer statistics and order history

### Product Management
- Organize products by categories and brands
- Set pricing and track stock levels
- Upload product images for better presentation

### Order Processing
- Create orders by selecting customers and products
- Track order status through the fulfillment process
- View order history and revenue analytics
- Automatic stock updates when orders are created

### File Management
- Upload payment proofs and contracts
- Store product images in blob storage
- Organize files by type and purpose

## API Endpoints

The system provides RESTful API endpoints for all major operations:

- `GET /api/customers` - List all customers
- `POST /api/customers` - Create new customer
- `GET /api/products` - List all products
- `POST /api/orders` - Create new order
- `GET /api/upload` - List uploaded files

## Security Features

- Input validation and sanitization
- File upload security (type and size restrictions)
- Anti-forgery token validation
- Secure file storage with access controls

## Performance Considerations

- Azure Table Storage for scalable data access
- Blob storage for efficient file management
- Queue-based messaging for async operations
- Denormalized data for improved query performance

## Troubleshooting

### Common Issues

1. **Storage Connection Error**
   - Verify Azure Storage account configuration
   - Check connection strings in `appsettings.json`
   - Ensure proper permissions are set

2. **File Upload Issues**
   - Check file size limits (10MB max)
   - Verify supported file types
   - Ensure blob container permissions

3. **Mock Service Issues**
   - Restart the application to reset mock data
   - Use the "Reinitialize Storage" button on dashboard
   - Check application logs for errors

### Support
For technical support or questions, please refer to the project documentation or contact the development team.

## License

This project is developed for educational purposes as part of the CLDV6212 course requirements.

## Version History

- **v1.0.0** - Initial release with Azure Storage integration
- Customer, Product, Order, and File Upload modules
- Responsive Bootstrap-based UI
- Mock Azure Storage service for development
- Comprehensive CRUD operations with queue messaging

---

**Developed by**: ST10439052  
**Course**: CLDV6212  
**Institution**: IIE MSA  
**Date**: 2024
