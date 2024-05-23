namespace SnapApp.Svc;

public static class EmailTemplates
{
    public const string verifyEmail = """
        <!DOCTYPE html>
        <html>
        <head>
        <meta http-equiv="Content-Type" content="text/html; charset=us-ascii">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Email Verification</title>
        </head>
        <body style="background-color:#84b1c9;">
            <div style="width: 835px; margin: auto; font-family: arial; color: #686868; padding: 10px 20px; background-color: #84b1c9;">
                <div style="background-color: #1e4708; padding-bottom: 5px; text-align: left; margin-bottom: 5px;">
                    <div style="margin-left: 20px;">
                        <img style="padding-top: 10px;" src="{{logoLink}}" alt="Snap Realty logo">
                    </div>
                </div>
                <div style="background-color: white; padding-top:15px; padding-bottom: 25px; text-align: left; margin-bottom: 5px; padding-left: 35px; padding-right: 35px;">
                    <p style="font-size: 26px; color: #1e4708; font-weight: bold; margin-bottom: 30px; text-transform: uppercase;">
                        <span>verify your email address</span>
                    </p>
                    <p style="margin-bottom: 8px; margin-top: 0px;">Hi {{recipientName}},</p>
                    <p style="margin-bottom: 60px; margin-top: 0px;">
                        Please click on the following button to verify your email address.
                    </p>
                    <div style="text-align: center;">
                        <a style="border: 1px solid #3fa5db; background-color: #3fa5db; color: white; border-radius: 10px; padding: 15px 65px; font-weight: bold; text-decoration: none; text-transform: uppercase;" href="{{verifyLink}}" target="_blank">
                            Verify
                        </a>
                    </div>
                    <p style="margin-top: 60px; margin-bottom: 0px;">
                        If you need assistance, please <a style="color: #3EAECE" href="https://snapnola.com/contact/" target="_blank">contact us</a>.
                    </p>
                </div>
                <div style="background-color: white; padding-top: 30px; padding-bottom: 25px; text-align: center; color: #686868;">
                    <p style="margin-bottom: 45px; margin-left: 30px; margin-right: 30px;">
                        Please add <span style="text-decoration: underline;">{{senderAddress}}</span> to your address book to ensure inbox delivery.
                    </p>
                    <p style="font-weight: bold; font-size: 18px; color: #686868;">
                        SNAPNOLA.COM
                    </p>
                    <p style="margin-bottom: 25px;">2625 General Pershing Street,New Orleans, LA 70115, United States</p>
                </div>
            </div>
        </body>
        </html>
        """;

    public const string inviteEmail = """
        <!DOCTYPE html>
        <html>
        <head>
        <meta http-equiv="Content-Type" content="text/html; charset=us-ascii">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Invitation</title>
        </head>
        <body style="background-color:#84b1c9;">
            <div style="width: 835px; margin: auto; font-family: arial; color: #686868; padding: 10px 20px; background-color: #84b1c9;">
                <div style="background-color: #1e4708; padding-bottom: 5px; text-align: left; margin-bottom: 5px;">
                    <div style="margin-left: 20px;">
                        <img style="padding-top: 10px;" src="{{logoLink}}" alt="Snap Realty logo">
                    </div>
                </div>
                <div style="background-color: white; padding-top:15px; padding-bottom: 25px; text-align: left; margin-bottom: 5px; padding-left: 35px; padding-right: 35px;">
                    <p style="font-size: 26px; color: #1e4708; font-weight: bold; margin-bottom: 30px; text-transform: uppercase;">
                        <span>you've been invited</span>
                    </p>
                    <p style="margin-bottom: 8px; margin-top: 0px;">Hi {{recipientName}},</p>
                    <p>
                        Realtor {{realtorName}} from {{companyName}} has invited to to join SnapApp.
                    </p>
                    <p>
                        Please <a style="color: #3EAECE" href="{{appStoreLink}}" target="_blank">download the app here</a>, then enter invite code <span style="text-transform: uppercase;">{{inviteCode}}</span> when prompted in the app.
                    </p>
                    <p style="margin-top: 30px; margin-bottom: 0px;">
                        If you need assistance, please <a style="color: #3EAECE" href="https://snapnola.com/contact/" target="_blank">contact us</a>.
                    </p>
                </div>
                <div style="background-color: white; padding-top: 30px; padding-bottom: 25px; text-align: center; color: #686868;">
                    <p style="margin-bottom: 45px; margin-left: 30px; margin-right: 30px;">
                        Please add <span style="text-decoration: underline;">{{senderAddress}}</span> to your address book to ensure inbox delivery.
                    </p>
                    <p style="font-weight: bold; font-size: 18px; color: #686868;">
                        SNAPNOLA.COM
                    </p>
                    <p style="margin-bottom: 25px;">2625 General Pershing Street,New Orleans, LA 70115, United States</p>
                </div>
            </div>
        </body>
        </html>
        """;
}