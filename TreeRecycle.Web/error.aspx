<%@ Page Title="Something went wrong" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="error.aspx.cs" Inherits="TreeRecycle.Web.ErrorPage" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="card narrow" style="margin:0 auto">
        <h1>Sorry, something went wrong</h1>
        <p>Please try again. If you were paying, check your email before paying again - your booking may already have gone through.</p>
        <p><a class="btn" href="<%: ResolveUrl("~/") %>">Back to home</a></p>
    </div>
</asp:Content>
