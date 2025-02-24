# Directives

Phoria Islands support a number of client directives that allow you to control when and how your Islands are hydrated on the client.

## Client directives

Unless a client directive is specified, the default behaviour is to server render the component only and not hydrate on the client. In this case there will be no additional JavaScript added to the page to render the component. Therefore we recommend to only use client directives if the component does require hydration.

### `Client.Load`

Hydrates the component immediately on page load.

```html
<phoria-island component="Counter" client="Client.Load"></phoria-island>
```

### `Client.Idle(int? timeout)`

Hydrates the component when the page has loaded and the `requestIdleCallback` event is fired.

Optionally you can specify a timeout in milliseconds after which the component will be hydrated regardless of whether the browser is idle.

```html
<phoria-island component="Counter" client="Client.Idle()"></phoria-island>
```

### `Client.Visible(string? rootMargin)`

Hydrates the component when the component has entered the user's viewport, which is determined using an `IntersectionObserver`.

Optionally you can specify a `rootMargin` value, which is a margin (in pixels) around the component that will trigger the hydration rather than the component itself.

```html
<phoria-island component="Counter" client="Client.Visible()"></phoria-island>
```

### `Client.Media(string mediaQuery)`

Hydrates the component when the specified CSS media query is matched.

```html
<phoria-island component="Counter" client="@(Client.Media("(max-width: 50em)"))"></phoria-island>
```

### `Client.Only`

Hydrates the component only on the client and does not server render the component. Hydration will occur immediately on page load, similar to `Client.Load`.

```html
<phoria-island component="Counter" client="Client.Only"></phoria-island>
```
