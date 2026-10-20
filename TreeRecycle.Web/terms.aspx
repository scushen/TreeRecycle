<%@ Page Title="Terms" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="terms.aspx.cs" Inherits="TreeRecycle.Web.Terms" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="card narrow" style="margin:0 auto">
        <h1>Terms of use</h1>
        <p class="msg warn">Draft for legal review before launch.</p>
        <p>This website is operated by Shanree Solutions Ltd, trading as <%: SiteName %> ("we"). We provide booking software. The collection itself is carried out by the club, school or charity you book ("the club").</p>
        <h2>Bookings and payment</h2>
        <p>When you book, you buy a collection service from the club. Payment is taken by Stripe on the club's behalf and paid to the club's own Stripe account. The booking fee shown at checkout is our charge for running the service.</p>
        <h2>On collection day</h2>
        <p>Your tree must be in an accessible outdoor spot (garden or driveway) by 8am on your collection day, with decorations, lights and stand removed. Clubs collect on the advertised day only. If the tree is not out when the crew calls, the club is not obliged to return and the fee is not refundable.</p>
        <h2>Refunds</h2>
        <p>Refunds are at the club's discretion. Contact the club directly using the details in your confirmation email.</p>
        <h2>Clubs</h2>
        <p>Clubs must be genuine, not-for-profit or community organisations (or businesses we have approved), give accurate details, collect every paid booking they accept, and handle customer data only for the collection. We may suspend any club at any time.</p>
    </div>
</asp:Content>
