<%@ Page Title="Register your club" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="register.aspx.cs" Inherits="TreeRecycle.Web.Register" %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <div class="grid grid-2" style="align-items:start">
        <div>
            <h1>Fundraise with Christmas tree collection</h1>
            <p>Register your club, school or charity. Once we've verified you, you get:</p>
            <ul>
                <li>Your own booking page to share on WhatsApp, Facebook and your website</li>
                <li>Card, Apple Pay and Google Pay payments straight into your club's Stripe account</li>
                <li>Automatic confirmation and reminder emails, in your words</li>
                <li>Collection lists grouped into trailer-sized runs by area, ready to print</li>
                <li>A running total of what you've raised</li>
            </ul>
            <p class="muted small">Customers pay a booking fee of <asp:Literal ID="litFee" runat="server" /> per tree on top of your price. Stripe's card processing fee comes out of your payout. There's nothing else to pay.</p>
            <p class="muted small">We verify every club before it can take payments, to protect the public. We may contact you or your governing body to confirm your details.</p>
        </div>

        <div class="card">
            <asp:PlaceHolder ID="phForm" runat="server">
                <asp:Literal ID="litError" runat="server" />
                <h2>Your club</h2>
                <div class="field">
                    <label for="<%= txtClubName.ClientID %>">Club / organisation name</label>
                    <asp:TextBox ID="txtClubName" runat="server" MaxLength="150" required="required" />
                </div>
                <div class="row2">
                    <div class="field">
                        <label for="<%= ddlType.ClientID %>">Type</label>
                        <asp:DropDownList ID="ddlType" runat="server" />
                    </div>
                    <div class="field">
                        <label for="<%= txtRegNo.ClientID %>">Registration no. (optional)</label>
                        <asp:TextBox ID="txtRegNo" runat="server" MaxLength="50" />
                        <div class="hint">Charity (RCN), CRO or governing-body club number.</div>
                    </div>
                </div>
                <div class="field">
                    <label for="<%= txtWebsite.ClientID %>">Website or Facebook page (optional)</label>
                    <asp:TextBox ID="txtWebsite" runat="server" MaxLength="250" />
                </div>
                <div class="field">
                    <label for="<%= txtAddress1.ClientID %>">Club address</label>
                    <asp:TextBox ID="txtAddress1" runat="server" MaxLength="150" required="required" />
                </div>
                <div class="field">
                    <asp:TextBox ID="txtAddress2" runat="server" MaxLength="150" placeholder="Address line 2 (optional)" aria-label="Address line 2" />
                </div>
                <div class="row3">
                    <div class="field">
                        <label for="<%= txtTown.ClientID %>">Town</label>
                        <asp:TextBox ID="txtTown" runat="server" MaxLength="100" required="required" />
                    </div>
                    <div class="field">
                        <label for="<%= ddlCounty.ClientID %>">County</label>
                        <asp:DropDownList ID="ddlCounty" runat="server" />
                    </div>
                    <div class="field">
                        <label for="<%= txtEircode.ClientID %>">Eircode</label>
                        <asp:TextBox ID="txtEircode" runat="server" MaxLength="10" required="required" style="text-transform:uppercase" />
                    </div>
                </div>
                <p class="hint" style="margin-top:-8px">Collections are measured from this Eircode, so use where your trailer sets out from.</p>

                <h2 class="mt">Contact and login</h2>
                <div class="row2">
                    <div class="field">
                        <label for="<%= txtContactName.ClientID %>">Contact name</label>
                        <asp:TextBox ID="txtContactName" runat="server" MaxLength="100" required="required" />
                    </div>
                    <div class="field">
                        <label for="<%= txtPhone.ClientID %>">Phone</label>
                        <asp:TextBox ID="txtPhone" runat="server" TextMode="Phone" MaxLength="30" required="required" />
                    </div>
                </div>
                <div class="field">
                    <label for="<%= txtEmail.ClientID %>">Club email (this is your login)</label>
                    <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="200" required="required" />
                    <div class="hint">Customers will see this address and can reply to it.</div>
                </div>
                <div class="row2">
                    <div class="field">
                        <label for="<%= txtPassword.ClientID %>">Password</label>
                        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="100" required="required" autocomplete="new-password" />
                        <div class="hint">At least 10 characters.</div>
                    </div>
                    <div class="field">
                        <label for="<%= txtPassword2.ClientID %>">Confirm password</label>
                        <asp:TextBox ID="txtPassword2" runat="server" TextMode="Password" MaxLength="100" required="required" autocomplete="new-password" />
                    </div>
                </div>
                <div class="field">
                    <label class="check"><asp:CheckBox ID="chkTerms" runat="server" /> <span>I'm authorised to register this organisation and I agree to the <a href="<%: ResolveUrl("~/terms.aspx") %>" target="_blank">terms</a>.</span></label>
                </div>
                <asp:Button ID="btnRegister" runat="server" Text="Register club" CssClass="btn big" OnClick="btnRegister_Click" />
            </asp:PlaceHolder>

            <asp:PlaceHolder ID="phDone" runat="server" Visible="false">
                <h2>Thanks - you're registered</h2>
                <p>We'll verify your club and email you, usually within one working day. In the meantime you can <a href="<%: ResolveUrl("~/club/") %>">log in</a> and set up your prices, collection dates and emails.</p>
            </asp:PlaceHolder>
        </div>
    </div>
</asp:Content>
