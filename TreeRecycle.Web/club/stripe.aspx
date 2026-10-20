<%@ Page Title="Payments" Language="C#" MasterPageFile="~/club/Club.Master" AutoEventWireup="true" CodeBehind="stripe.aspx.cs" Inherits="TreeRecycle.Web.ClubArea.StripeConnect" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h1>Getting paid</h1>
    <asp:Literal ID="litMsg" runat="server" />
    <div class="card narrow">
        <p>Bookings are paid by card, Apple Pay or Google Pay through <strong>Stripe</strong>, straight into your club's own Stripe account. Stripe pays out to your club's bank account. We never hold your money.</p>
        <p class="small muted">Stripe will ask for your club's details, a contact's ID, and the bank account to pay into. This is a legal requirement for anyone taking card payments. Stripe's standard EU card fee comes out of each payment.</p>
        <h2 class="mt">Status: <asp:Literal ID="litStatus" runat="server" /></h2>
        <div class="actions mt">
            <asp:Button ID="btnConnect" runat="server" CssClass="btn big" OnClick="btnConnect_Click" />
            <asp:HyperLink ID="lnkDashboard" runat="server" CssClass="btn light" Target="_blank" Text="Open your Stripe dashboard" />
        </div>
    </div>
</asp:Content>
