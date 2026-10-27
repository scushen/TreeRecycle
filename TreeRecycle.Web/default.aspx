<%@ Page Title="Christmas tree collection from your door" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="TreeRecycle.Web._Default" %>
<asp:Content ContentPlaceHolderID="HeadContent" runat="server">
    <meta name="description" content="Book a local club to collect your Christmas tree from your garden. Enter your postcode to find clubs collecting near you." />
</asp:Content>
<asp:Content ContentPlaceHolderID="HeroContent" runat="server">
    <section class="hero">
        <div class="wrap">
            <h1>Your Christmas tree, collected from your garden</h1>
            <p class="lead">Local clubs and organisations collect your old tree after Christmas and take it for recycling. No pine needles in the car, and every booking supports a local club.</p>
            <asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnSearch" CssClass="searchbox">
                <asp:DropDownList ID="ddlCountry" runat="server" CssClass="country" aria-label="Country" onchange="trSetCountry(this.value)" />
                <asp:TextBox ID="txtPostcode" runat="server" MaxLength="12" aria-label="Your postcode" />
                <asp:Button ID="btnSearch" runat="server" Text="Find clubs near me" CssClass="btn berry big" OnClick="btnSearch_Click" />
            </asp:Panel>
        </div>
    </section>
</asp:Content>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="card lift">
        <asp:Literal ID="litSearchMsg" runat="server" />
        <asp:PlaceHolder ID="phResults" runat="server" Visible="false">
            <h2><asp:Literal ID="litResultsTitle" runat="server" /></h2>
            <ul class="clublist">
                <asp:Repeater ID="rptResults" runat="server" ItemType="TreeRecycle.Web.ClubResult">
                    <ItemTemplate>
                        <li class="clubrow">
                            <div>
                                <h3><%#: Item.Name %></h3>
                                <div class="muted small"><%#: Item.Place %><%# Item.DistanceText == "" ? "" : " &middot; " + Enc(Item.DistanceText) %></div>
                                <div class="small mt" style="margin-top:6px"><%#: Item.PriceFrom %> &middot; <%#: Item.NextDate %></div>
                            </div>
                            <a class="btn" href="<%#: Item.Url %>">Book collection</a>
                        </li>
                    </ItemTemplate>
                </asp:Repeater>
            </ul>
        </asp:PlaceHolder>

        <h2 class="mt">Or browse by area</h2>
        <div class="filters">
            <div class="field">
                <label for="<%= ddlBrowseCountry.ClientID %>">Country</label>
                <asp:DropDownList ID="ddlBrowseCountry" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlBrowseCountry_Changed" />
            </div>
            <div class="field">
                <label for="<%= ddlRegion.ClientID %>"><asp:Literal ID="litRegionLabel" runat="server" /></label>
                <asp:DropDownList ID="ddlRegion" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlRegion_Changed" />
            </div>
            <div class="field">
                <label for="<%= ddlTown.ClientID %>">Town / area</label>
                <asp:DropDownList ID="ddlTown" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlTown_Changed" />
            </div>
        </div>
        <asp:PlaceHolder ID="phBrowse" runat="server" Visible="false">
            <ul class="clublist">
                <asp:Repeater ID="rptBrowse" runat="server" ItemType="TreeRecycle.Web.ClubResult">
                    <ItemTemplate>
                        <li class="clubrow">
                            <div>
                                <h3><%#: Item.Name %></h3>
                                <div class="muted small"><%#: Item.Place %> &middot; <%#: Item.RadiusText %></div>
                                <div class="small" style="margin-top:6px"><%#: Item.PriceFrom %> &middot; <%#: Item.NextDate %></div>
                            </div>
                            <a class="btn" href="<%#: Item.Url %>">Book collection</a>
                        </li>
                    </ItemTemplate>
                </asp:Repeater>
            </ul>
        </asp:PlaceHolder>
        <asp:Literal ID="litBrowseMsg" runat="server" />
    </div>

    <div class="grid grid-2 mt">
        <div class="card">
            <h2>How it works</h2>
            <ol class="steps">
                <li>Enter your postcode and pick a club that collects in your area.</li>
                <li>Choose your tree size and pay securely by card, Apple Pay or Google Pay.</li>
                <li>Get a confirmation email, and a reminder the day before collection.</li>
                <li>Leave the tree in your garden or driveway. The club does the rest.</li>
            </ol>
        </div>
        <div class="card">
            <h2>Run a club or organisation?</h2>
            <p>Tree collection is one of the easiest earners of the season. We give you an online booking page, take the payments, plan your routes and print your collection sheets. No more WhatsApp lists and bank transfers.</p>
            <a class="btn light" href="<%: ResolveUrl("~/register.aspx") %>">Register your club</a>
        </div>
    </div>
</asp:Content>
<asp:Content ContentPlaceHolderID="Scripts" runat="server">
    <script>
    // Placeholder follows the chosen country: "Your Eircode, e.g. A65 F4E2" / "Your postcode, e.g. SW1A 1AA"...
    var trPostcodeHints = <%= PostcodeHintsJson %>;
    function trSetCountry(code) {
        var box = document.getElementById('<%= txtPostcode.ClientID %>');
        var hint = trPostcodeHints[code];
        if (box && hint) { box.placeholder = hint; box.setAttribute('aria-label', hint); }
    }
    trSetCountry(document.getElementById('<%= ddlCountry.ClientID %>').value);
    </script>
</asp:Content>
