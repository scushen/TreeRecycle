<%@ Page Title="Book a tree collection" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="club.aspx.cs" Inherits="TreeRecycle.Web.ClubOrder" %>
<asp:Content ContentPlaceHolderID="HeadContent" runat="server">
    <asp:Literal ID="litMeta" runat="server" />
</asp:Content>
<asp:Content ContentPlaceHolderID="HeroContent" runat="server">
    <section class="hero" style="padding:40px 0 64px">
        <div class="wrap">
            <h1><asp:Literal ID="litClubName" runat="server" /></h1>
            <p class="lead"><asp:Literal ID="litPlace" runat="server" /></p>
        </div>
    </section>
</asp:Content>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <asp:PlaceHolder ID="phNotFound" runat="server" Visible="false">
        <div class="card lift narrow">
            <h2>Club not found</h2>
            <p>We couldn't find that club. <a href="<%: ResolveUrl("~/") %>">Search by postcode</a> to find a club near you.</p>
        </div>
    </asp:PlaceHolder>

    <asp:PlaceHolder ID="phClub" runat="server">
        <div class="grid grid-2 lift" style="align-items:start">
            <div class="card">
                <asp:Literal ID="litClosed" runat="server" />
                <asp:Literal ID="litError" runat="server" />

                <h2>1. Your tree</h2>
                <div class="field">
                    <span class="label">Tree size</span>
                    <ul class="sizes">
                        <asp:Repeater ID="rptSizes" runat="server" ItemType="TreeRecycle.DataAccessObjects.ClubPrice">
                            <ItemTemplate>
                                <li><label>
                                    <input type="radio" name="size" value="<%# Item.ClubPriceId %>" data-price="<%# Item.Price.ToString(System.Globalization.CultureInfo.InvariantCulture) %>" <%# Item.ClubPriceId == SelectedPriceId ? "checked" : "" %> required />
                                    <span><%#: Item.Label %><br /><span class="muted"><%#: PriceText(Item.Price) %></span></span>
                                </label></li>
                            </ItemTemplate>
                        </asp:Repeater>
                    </ul>
                </div>
                <div class="row2">
                    <div class="field">
                        <label for="<%= ddlQty.ClientID %>">How many trees?</label>
                        <asp:DropDownList ID="ddlQty" runat="server">
                            <asp:ListItem Value="1" Selected="True">1</asp:ListItem>
                            <asp:ListItem Value="2">2</asp:ListItem>
                            <asp:ListItem Value="3">3</asp:ListItem>
                            <asp:ListItem Value="4">4</asp:ListItem>
                            <asp:ListItem Value="5">5</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="field">
                        <label for="<%= ddlDate.ClientID %>">Collection day</label>
                        <asp:DropDownList ID="ddlDate" runat="server" />
                    </div>
                </div>

                <h2 class="mt">2. Where is it?</h2>
                <div class="field">
                    <label for="<%= txtPostcode.ClientID %>"><asp:Literal ID="litPostcodeLabel" runat="server" /></label>
                    <asp:TextBox ID="txtPostcode" runat="server" MaxLength="12" style="text-transform:uppercase;max-width:200px" required="required" />
                    <div class="hint"><asp:Literal ID="litPostcodeHint" runat="server" /></div>
                </div>
                <div class="field">
                    <label for="<%= txtAddress1.ClientID %>">Address</label>
                    <asp:TextBox ID="txtAddress1" runat="server" MaxLength="150" required="required" />
                </div>
                <div class="field">
                    <asp:TextBox ID="txtAddress2" runat="server" MaxLength="150" placeholder="Address line 2 (optional)" aria-label="Address line 2" />
                </div>
                <div class="field">
                    <label for="<%= txtTown.ClientID %>">Town / area</label>
                    <asp:TextBox ID="txtTown" runat="server" MaxLength="100" required="required" />
                </div>
                <div class="field">
                    <label for="<%= txtNotes.ClientID %>">Where will the tree be? (optional)</label>
                    <asp:TextBox ID="txtNotes" runat="server" TextMode="MultiLine" MaxLength="500" Rows="2" style="min-height:70px" placeholder="e.g. by the front gate, house with the red door" />
                </div>

                <h2 class="mt">3. Your details</h2>
                <div class="field">
                    <label for="<%= txtName.ClientID %>">Name</label>
                    <asp:TextBox ID="txtName" runat="server" MaxLength="100" required="required" autocomplete="name" />
                </div>
                <div class="row2">
                    <div class="field">
                        <label for="<%= txtEmail.ClientID %>">Email</label>
                        <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="200" required="required" autocomplete="email" />
                    </div>
                    <div class="field">
                        <label for="<%= txtPhone.ClientID %>">Mobile</label>
                        <asp:TextBox ID="txtPhone" runat="server" TextMode="Phone" MaxLength="30" required="required" autocomplete="tel" />
                    </div>
                </div>
                <div class="field">
                    <label class="check"><asp:CheckBox ID="chkTerms" runat="server" /> <span>I'll have my tree out by 8am on the collection day, with decorations and stand removed. If it isn't out, the club won't come back and the fee isn't refunded. I agree to the <a href="<%: ResolveUrl("~/terms.aspx") %>" target="_blank">terms</a>.</span></label>
                </div>

                <div class="total" id="totalBox">
                    <div><span id="tLineLabel">Collection</span><span id="tLine">-</span></div>
                    <div><span>Booking fee</span><span id="tFee">-</span></div>
                    <div class="sum"><span>Total</span><span id="tTotal">-</span></div>
                </div>
                <div class="actions mt">
                    <asp:Button ID="btnPay" runat="server" Text="Continue to secure payment" CssClass="btn berry big" OnClick="btnPay_Click" />
                </div>
                <p class="hint">You'll pay on Stripe's secure page. Payment goes to <asp:Literal ID="litPayee" runat="server" />.</p>
            </div>

            <div>
                <div class="card">
                    <h2>About this collection</h2>
                    <asp:Literal ID="litDescription" runat="server" />
                    <h3 class="mt">Collection days</h3>
                    <asp:Literal ID="litDates" runat="server" />
                    <asp:Literal ID="litCollectionInfo" runat="server" />
                    <p class="small muted mt">Collects within <asp:Literal ID="litRadius" runat="server" /> of the club.</p>
                </div>
                <div class="card">
                    <h3>Questions?</h3>
                    <p class="mb0"><asp:Literal ID="litContact" runat="server" /></p>
                </div>
            </div>
        </div>
    </asp:PlaceHolder>
</asp:Content>
<asp:Content ContentPlaceHolderID="Scripts" runat="server">
    <script>
    (function () {
        var fee = <%= FeePerTreeJs %>;
        var qty = document.getElementById('<%= ddlQty.ClientID %>');
        var symbol = <%= CurrencySymbolJs %>;
        function money(n) { return symbol + n.toFixed(2); }
        function update() {
            var picked = document.querySelector('input[name=size]:checked');
            var q = parseInt(qty.value, 10) || 1;
            if (!picked) return;
            var price = parseFloat(picked.getAttribute('data-price'));
            document.getElementById('tLineLabel').textContent = q + ' x tree collection';
            document.getElementById('tLine').textContent = money(price * q);
            document.getElementById('tFee').textContent = money(fee * q);
            document.getElementById('tTotal').textContent = money((price + fee) * q);
        }
        Array.prototype.forEach.call(document.querySelectorAll('input[name=size]'), function (r) { r.addEventListener('change', update); });
        if (qty) { qty.addEventListener('change', update); update(); }
    })();
    </script>
</asp:Content>
