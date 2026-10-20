<%@ Page Title="Club details" Language="C#" MasterPageFile="~/club/Club.Master" AutoEventWireup="true" CodeBehind="details.aspx.cs" Inherits="TreeRecycle.Web.ClubArea.Details" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h1>Club details</h1>
    <asp:Literal ID="litMsg" runat="server" />
    <div class="grid grid-2" style="align-items:start">
        <div class="card">
            <h2>Bookings</h2>
            <div class="field">
                <label class="check"><asp:CheckBox ID="chkAccepting" runat="server" /> <span><strong>Accepting orders</strong><br /><span class="hint">Untick to pause your booking page at any time.</span></span></label>
            </div>
            <div class="field">
                <label for="<%= txtCloseDate.ClientID %>">Stop taking orders after (optional)</label>
                <asp:TextBox ID="txtCloseDate" runat="server" TextMode="Date" />
                <div class="hint">Bookings close automatically at midnight on this day.</div>
            </div>
            <div class="row2">
                <div class="field">
                    <label for="<%= txtRadius.ClientID %>">Collection radius (km)</label>
                    <asp:TextBox ID="txtRadius" runat="server" TextMode="Number" min="1" max="100" />
                    <div class="hint">Straight-line distance from your club's Eircode.</div>
                </div>
                <div class="field">
                    <label for="<%= txtCapacity.ClientID %>">Trees per trailer load</label>
                    <asp:TextBox ID="txtCapacity" runat="server" TextMode="Number" min="1" max="500" />
                    <div class="hint">Used to split collection day into runs.</div>
                </div>
            </div>
            <div class="field">
                <label for="<%= txtDescription.ClientID %>">About your club (shown on your booking page)</label>
                <asp:TextBox ID="txtDescription" runat="server" TextMode="MultiLine" MaxLength="2000" />
            </div>
            <div class="field">
                <label for="<%= txtCollectionInfo.ClientID %>">Collection details (booking page and confirmation email)</label>
                <asp:TextBox ID="txtCollectionInfo" runat="server" TextMode="MultiLine" MaxLength="2000" placeholder="e.g. Crews start at 9am and work outwards from the club. Trees are chipped and used for mulch at the club grounds." />
            </div>
        </div>

        <div class="card">
            <h2>Club</h2>
            <div class="field">
                <label for="<%= txtName.ClientID %>">Name</label>
                <asp:TextBox ID="txtName" runat="server" MaxLength="150" />
            </div>
            <div class="row2">
                <div class="field">
                    <label for="<%= ddlType.ClientID %>">Type</label>
                    <asp:DropDownList ID="ddlType" runat="server" />
                </div>
                <div class="field">
                    <label for="<%= txtRegNo.ClientID %>">Registration no.</label>
                    <asp:TextBox ID="txtRegNo" runat="server" MaxLength="50" />
                </div>
            </div>
            <div class="field">
                <label for="<%= txtWebsite.ClientID %>">Website</label>
                <asp:TextBox ID="txtWebsite" runat="server" MaxLength="250" />
            </div>
            <div class="field">
                <label for="<%= txtAddress1.ClientID %>">Address</label>
                <asp:TextBox ID="txtAddress1" runat="server" MaxLength="150" />
            </div>
            <div class="field">
                <asp:TextBox ID="txtAddress2" runat="server" MaxLength="150" aria-label="Address line 2" />
            </div>
            <div class="row3">
                <div class="field">
                    <label for="<%= txtTown.ClientID %>">Town</label>
                    <asp:TextBox ID="txtTown" runat="server" MaxLength="100" />
                </div>
                <div class="field">
                    <label for="<%= ddlCounty.ClientID %>">County</label>
                    <asp:DropDownList ID="ddlCounty" runat="server" />
                </div>
                <div class="field">
                    <label for="<%= txtEircode.ClientID %>">Eircode</label>
                    <asp:TextBox ID="txtEircode" runat="server" MaxLength="10" style="text-transform:uppercase" />
                </div>
            </div>
            <p class="hint" style="margin-top:-8px"><asp:Literal ID="litGeo" runat="server" /></p>

            <h2 class="mt">Contact</h2>
            <div class="row2">
                <div class="field">
                    <label for="<%= txtContactName.ClientID %>">Contact name</label>
                    <asp:TextBox ID="txtContactName" runat="server" MaxLength="100" />
                </div>
                <div class="field">
                    <label for="<%= txtPhone.ClientID %>">Phone</label>
                    <asp:TextBox ID="txtPhone" runat="server" MaxLength="30" />
                </div>
            </div>
            <div class="field">
                <label for="<%= txtEmail.ClientID %>">Email (login)</label>
                <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="200" />
            </div>
        </div>
    </div>
    <div class="actions">
        <asp:Button ID="btnSave" runat="server" Text="Save details" CssClass="btn big" OnClick="btnSave_Click" />
    </div>

    <div class="card mt narrow">
        <h2>Change password</h2>
        <div class="row2">
            <div class="field">
                <label for="<%= txtNewPassword.ClientID %>">New password</label>
                <asp:TextBox ID="txtNewPassword" runat="server" TextMode="Password" MaxLength="100" autocomplete="new-password" />
            </div>
            <div class="field">
                <label for="<%= txtNewPassword2.ClientID %>">Confirm</label>
                <asp:TextBox ID="txtNewPassword2" runat="server" TextMode="Password" MaxLength="100" autocomplete="new-password" />
            </div>
        </div>
        <asp:Button ID="btnPassword" runat="server" Text="Change password" CssClass="btn light" OnClick="btnPassword_Click" />
    </div>
</asp:Content>
