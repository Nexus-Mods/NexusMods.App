# Linux Protocol Registration

## AppImage

If you're using the AppImage version of the app you need to make sure the app is integrated into your desktop environment properly.
You can use [gearlever](https://github.com/mijorus/gearlever) to manage the AppImage version of the app.

## Verify

You can verify whether the app is registered correctly using various methods:

1) In your browser, open `nxm://foo`. This should open the app.
2) In your terminal, query the default application: `xdg-mime query default x-scheme-handler/nxm`.
3) In your terminal, run `xdg-open "nxm://foo"`. This should open the app.
4) In the app, open the protocol registration test page and run the test.
