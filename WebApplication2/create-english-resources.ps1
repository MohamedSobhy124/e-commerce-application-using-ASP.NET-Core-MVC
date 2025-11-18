# PowerShell script to create English resource file
# Run this from the WebApplication2 directory

$englishResources = @'
<?xml version="1.0" encoding="utf-8"?>
<root>
  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:import namespace="http://www.w3.org/XML/1998/namespace" />
    <xsd:element name="root" msdata:IsDataSet="true">
      <xsd:complexType>
        <xsd:choice maxOccurs="unbounded">
          <xsd:element name="metadata">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" />
              </xsd:sequence>
              <xsd:attribute name="name" use="required" type="xsd:string" />
              <xsd:attribute name="type" type="xsd:string" />
              <xsd:attribute name="mimetype" type="xsd:string" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="assembly">
            <xsd:complexType>
              <xsd:attribute name="alias" type="xsd:string" />
              <xsd:attribute name="name" type="xsd:string" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="data">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" msdata:Ordinal="1" />
              <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
              <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
              <xsd:attribute ref="xml:space" msdata:Ordinal="5" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="resheader">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  
  <!-- Navigation & Common -->
  <data name="Home" xml:space="preserve"><value>Home</value></data>
  <data name="Shop" xml:space="preserve"><value>Shop</value></data>
  <data name="Products" xml:space="preserve"><value>Products</value></data>
  <data name="Cart" xml:space="preserve"><value>Cart</value></data>
  <data name="Checkout" xml:space="preserve"><value>Checkout</value></data>
  <data name="Orders" xml:space="preserve"><value>Orders</value></data>
  <data name="MyAccount" xml:space="preserve"><value>My Account</value></data>
  <data name="Login" xml:space="preserve"><value>Login</value></data>
  <data name="Register" xml:space="preserve"><value>Register</value></data>
  <data name="Logout" xml:space="preserve"><value>Logout</value></data>
  <data name="Management" xml:space="preserve"><value>Management</value></data>
  <data name="Dashboard" xml:space="preserve"><value>Dashboard</value></data>
  
  <!-- Product Related -->
  <data name="ProductDetails" xml:space="preserve"><value>Product Details</value></data>
  <data name="Price" xml:space="preserve"><value>Price</value></data>
  <data name="Author" xml:space="preserve"><value>Author</value></data>
  <data name="Category" xml:space="preserve"><value>Category</value></data>
  <data name="Description" xml:space="preserve"><value>Description</value></data>
  <data name="AddToCart" xml:space="preserve"><value>Add to Cart</value></data>
  <data name="BuyNow" xml:space="preserve"><value>Buy Now</value></data>
  <data name="ViewDetails" xml:space="preserve"><value>View Details</value></data>
  <data name="InStock" xml:space="preserve"><value>In Stock</value></data>
  <data name="OutOfStock" xml:space="preserve"><value>Out of Stock</value></data>
  <data name="Quantity" xml:space="preserve"><value>Quantity</value></data>
  
  <!-- Cart & Checkout -->
  <data name="ShoppingCart" xml:space="preserve"><value>Shopping Cart</value></data>
  <data name="CartIsEmpty" xml:space="preserve"><value>Your cart is empty</value></data>
  <data name="ContinueShopping" xml:space="preserve"><value>Continue Shopping</value></data>
  <data name="ProceedToCheckout" xml:space="preserve"><value>Proceed to Checkout</value></data>
  <data name="Subtotal" xml:space="preserve"><value>Subtotal</value></data>
  <data name="Total" xml:space="preserve"><value>Total</value></data>
  <data name="OrderSummary" xml:space="preserve"><value>Order Summary</value></data>
  <data name="ShippingDetails" xml:space="preserve"><value>Shipping Details</value></data>
  <data name="Name" xml:space="preserve"><value>Name</value></data>
  <data name="Email" xml:space="preserve"><value>Email</value></data>
  <data name="Phone" xml:space="preserve"><value>Phone</value></data>
  <data name="Address" xml:space="preserve"><value>Address</value></data>
  <data name="City" xml:space="preserve"><value>City</value></data>
  <data name="State" xml:space="preserve"><value>State</value></data>
  <data name="PostalCode" xml:space="preserve"><value>Postal Code</value></data>
  <data name="PlaceOrder" xml:space="preserve"><value>Place Order</value></data>
  <data name="PaymentMethod" xml:space="preserve"><value>Payment Method</value></data>
  <data name="StreetAddress" xml:space="preserve"><value>Street Address</value></data>
  
  <!-- Order Status -->
  <data name="OrderStatus" xml:space="preserve"><value>Order Status</value></data>
  <data name="Pending" xml:space="preserve"><value>Pending</value></data>
  <data name="Approved" xml:space="preserve"><value>Approved</value></data>
  <data name="Processing" xml:space="preserve"><value>Processing</value></data>
  <data name="Shipped" xml:space="preserve"><value>Shipped</value></data>
  <data name="Delivered" xml:space="preserve"><value>Delivered</value></data>
  <data name="Cancelled" xml:space="preserve"><value>Cancelled</value></data>
  <data name="PaymentStatus" xml:space="preserve"><value>Payment Status</value></data>
  <data name="Paid" xml:space="preserve"><value>Paid</value></data>
  
  <!-- Admin Panel -->
  <data name="Categories" xml:space="preserve"><value>Categories</value></data>
  <data name="AddNew" xml:space="preserve"><value>Add New</value></data>
  <data name="Edit" xml:space="preserve"><value>Edit</value></data>
  <data name="Delete" xml:space="preserve"><value>Delete</value></data>
  <data name="Save" xml:space="preserve"><value>Save</value></data>
  <data name="Cancel" xml:space="preserve"><value>Cancel</value></data>
  <data name="Actions" xml:space="preserve"><value>Actions</value></data>
  <data name="Details" xml:space="preserve"><value>Details</value></data>
  <data name="Update" xml:space="preserve"><value>Update</value></data>
  <data name="Create" xml:space="preserve"><value>Create</value></data>
  <data name="Company" xml:space="preserve"><value>Company</value></data>
  <data name="Product" xml:space="preserve"><value>Product</value></data>
  
  <!-- Messages -->
  <data name="Success" xml:space="preserve"><value>Success</value></data>
  <data name="Error" xml:space="preserve"><value>Error</value></data>
  <data name="Warning" xml:space="preserve"><value>Warning</value></data>
  <data name="ItemAddedToCart" xml:space="preserve"><value>Product added to cart successfully</value></data>
  <data name="ItemRemovedFromCart" xml:space="preserve"><value>Product removed from cart</value></data>
  <data name="OrderPlacedSuccessfully" xml:space="preserve"><value>Order placed successfully!</value></data>
  <data name="CartUpdatedSuccessfully" xml:space="preserve"><value>Cart updated successfully</value></data>
  
  <!-- Search & Filter -->
  <data name="Search" xml:space="preserve"><value>Search</value></data>
  <data name="Filter" xml:space="preserve"><value>Filter</value></data>
  <data name="SortBy" xml:space="preserve"><value>Sort By</value></data>
  <data name="AllCategories" xml:space="preserve"><value>All Categories</value></data>
  <data name="PriceLowToHigh" xml:space="preserve"><value>Price: Low to High</value></data>
  <data name="PriceHighToLow" xml:space="preserve"><value>Price: High to Low</value></data>
  <data name="Newest" xml:space="preserve"><value>Newest</value></data>
  <data name="ClearFilters" xml:space="preserve"><value>Clear Filters</value></data>
  <data name="ShowingResults" xml:space="preserve"><value>Showing</value></data>
  <data name="Books" xml:space="preserve"><value>books</value></data>
  
  <!-- Footer -->
  <data name="AboutUs" xml:space="preserve"><value>About Us</value></data>
  <data name="ContactUs" xml:space="preserve"><value>Contact Us</value></data>
  <data name="PrivacyPolicy" xml:space="preserve"><value>Privacy Policy</value></data>
  <data name="TermsAndConditions" xml:space="preserve"><value>Terms and Conditions</value></data>
  <data name="ReturnPolicy" xml:space="preserve"><value>Return Policy</value></data>
  <data name="Newsletter" xml:space="preserve"><value>Newsletter</value></data>
  <data name="Subscribe" xml:space="preserve"><value>Subscribe</value></data>
  <data name="AllRightsReserved" xml:space="preserve"><value>All rights reserved</value></data>
  <data name="QuickLinks" xml:space="preserve"><value>Quick Links</value></data>
  <data name="CustomerService" xml:space="preserve"><value>Customer Service</value></data>
  <data name="HelpCenter" xml:space="preserve"><value>Help Center</value></data>
  <data name="ShippingInfo" xml:space="preserve"><value>Shipping Info</value></data>
  <data name="Returns" xml:space="preserve"><value>Returns</value></data>
  
  <!-- Guest Checkout -->
  <data name="GuestCheckout" xml:space="preserve"><value>Guest Checkout</value></data>
  <data name="TrackOrder" xml:space="preserve"><value>Track Order</value></data>
  <data name="OrderNumber" xml:space="preserve"><value>Order Number</value></data>
  <data name="EmailAddress" xml:space="preserve"><value>Email Address</value></data>
  <data name="TrackYourOrder" xml:space="preserve"><value>Track Your Order</value></data>
  
  <!-- Common Actions -->
  <data name="Remove" xml:space="preserve"><value>Remove</value></data>
  <data name="Add" xml:space="preserve"><value>Add</value></data>
  <data name="Back" xml:space="preserve"><value>Back</value></data>
  <data name="Next" xml:space="preserve"><value>Next</value></data>
  <data name="Previous" xml:space="preserve"><value>Previous</value></data>
  <data name="Close" xml:space="preserve"><value>Close</value></data>
  <data name="Confirm" xml:space="preserve"><value>Confirm</value></data>
  <data name="View" xml:space="preserve"><value>View</value></data>
  
  <!-- Language Switcher -->
  <data name="Language" xml:space="preserve"><value>Language</value></data>
  <data name="Arabic" xml:space="preserve"><value>Arabic</value></data>
  <data name="English" xml:space="preserve"><value>English</value></data>
  
  <!-- Order Actions -->
  <data name="StartProcessing" xml:space="preserve"><value>Start Processing</value></data>
  <data name="ShipOrder" xml:space="preserve"><value>Ship Order</value></data>
  <data name="MarkAsDelivered" xml:space="preserve"><value>Mark as Delivered</value></data>
  <data name="CancelOrder" xml:space="preserve"><value>Cancel Order</value></data>
  <data name="UpdateOrderDetails" xml:space="preserve"><value>Update Order Details</value></data>
  <data name="OrderItems" xml:space="preserve"><value>Order Items</value></data>
  <data name="CustomerInformation" xml:space="preserve"><value>Customer Information</value></data>
  <data name="ShippingInformation" xml:space="preserve"><value>Shipping Information</value></data>
  <data name="Carrier" xml:space="preserve"><value>Carrier</value></data>
  <data name="TrackingNumber" xml:space="preserve"><value>Tracking Number</value></data>
  
  <!-- Additional Footer Keys -->
  <data name="QuickLinks" xml:space="preserve"><value>Quick Links</value></data>
  <data name="CustomerService" xml:space="preserve"><value>Customer Service</value></data>
  <data name="HelpCenter" xml:space="preserve"><value>Help Center</value></data>
  <data name="ShippingInfo" xml:space="preserve"><value>Shipping Info</value></data>
  <data name="Returns" xml:space="preserve"><value>Returns</value></data>
  <data name="ClearFilters" xml:space="preserve"><value>Clear Filters</value></data>
  <data name="ShowingResults" xml:space="preserve"><value>Showing</value></data>
  <data name="Books" xml:space="preserve"><value>books</value></data>
  <data name="EmailAddress" xml:space="preserve"><value>Email Address</value></data>
  <data name="TrackYourOrder" xml:space="preserve"><value>Track Your Order</value></data>
  <data name="View" xml:space="preserve"><value>View</value></data>
</root>
'@

# Create Resources directory if it doesn't exist
if (-not (Test-Path "Resources")) {
    New-Item -ItemType Directory -Path "Resources" -Force
    Write-Host "Created Resources directory" -ForegroundColor Green
}

# Write the English resource file
$englishResources | Out-File -FilePath "Resources\SharedResources.en.resx" -Encoding UTF8
Write-Host "✅ English resource file created successfully!" -ForegroundColor Green
Write-Host "Location: Resources\SharedResources.en.resx" -ForegroundColor Cyan

