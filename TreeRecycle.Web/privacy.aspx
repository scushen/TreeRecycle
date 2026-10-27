<%@ Page Title="Privacy" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="privacy.aspx.cs" Inherits="TreeRecycle.Web.Privacy" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="card narrow" style="margin:0 auto">
        <h1>Privacy notice</h1>
        <p class="msg warn">Draft for legal review before launch.</p>
        <p>Shanree Solutions Ltd, trading as <%: SiteName %>, operates this website. When you book a collection we collect your name, email, phone number, address and postcode, and share them with the club you book so it can collect your tree and contact you about it.</p>
        <p>We use Stripe to take payments (we never see your card details), Postmark to send emails and Google Maps to place your address on the map.</p>
        <p>We keep booking details until the end of the collection season plus 12 months for accounting and dispute purposes, then delete them. To access or delete your data, email us.</p>
    </div>
</asp:Content>
