# Creating Phoria Island components

A Phoria Island allows you to easily and efficiently render islands of interactivity using modern UI frameworks within your dotnet web app (Razor Pages or MVC) using both Client Side Rendering (CSR) and/or Server Side Rendering (SSR).

The Phoria NuGet packages includes a `PhoriaIslandTagHelper`, which can be used to render any Phoria Island, for example:

```html
<phoria-island component="Counter" client="Client.Load" props="new { StartAt = 5 }"></phoria-island>
```

Also included is a `PhoriaIslandComponentFactory` that can be used to create your own Tag Helpers or View Components to render a specific Phoria Island component, for example:

```html
<counter start-at="5" />
```

This guide will walk you through both methods of creating Phoria Island components.

## Create the UI component

> [!IMPORTANT]
> For the purpose of this guide we will assume that you have created your Phoria app using one of the [example projects](./getting-started.md#clone-an-example-project) and the sample code is based on the `framework-react` example.
> 
> The same principles apply to any Phoria project, but you may need to adjust the code samples or file paths to suit your own project.

First of all you will need to create your UI component. We will create a simple counter component in a new file `WebApp/ui/src/components/Counter/Counter.tsx`:

```tsx
import { useState } from "react"

interface CounterProps {
  startAt?: number
}

function Counter({ startAt }: CounterProps) {
  const [count, setCount] = useState(startAt ?? 0)

  return (
    <div>
      <button type="button" onClick={() => setCount((value) => value + 1)}>
        count is {count}
      </button>
    </div>
  )
}

export { Counter }
```

> [!TIP]
> You may wish to use a tool such as [Storybook](https://storybook.js.org/) when developing your UI components, which allows you to develop and test UI components in isolation so that you can ensure they are functioning as expected before using them in your Phoria app.

## Register the UI component

You will then need to [register the component](./component-register.md) so that your Phoria Island's know where to import it from. Edit the file `WebApp/ui/src/components/register.ts`:

```ts
import { registerComponents } from "@phoria/phoria"

registerComponents({
  Counter: {
    loader: {
      module: () => import("./Counter/Counter.tsx"),
      component: (module) => module.Counter
    },
    framework: "react"
  }
})
```

This tells Phoria to register a component using the name `Counter`, which can be imported from `./Counter/Counter.tsx` using a named export `Counter`, and it uses the `react` framework.

If your component uses a default export you can register it like this instead:

```ts
import { registerComponents } from "@phoria/phoria"

registerComponents({
  Counter: {
    loader: () => import("./Counter/Counter.tsx"),
    framework: "react"
  }
})
```

> [!TIP]
> The object passed to `registerComponents` can be used to register multiple components so as you add components that you want to use in Phoria Islands you can just keep adding them here.

## Render the UI component with the `PhoriaIslandTagHelper`

Now you can render your component with a Phoria Island. Open any Razor Page or MVC view and add the following code:

```html
<phoria-island component="Counter" client="Client.Load" props="new { StartAt = 5 }"></phoria-island>
```

This will render component registered with the name `Counter`, server render the component and hydrate the component on page load, and set the `startAt` component prop to `5`.

To explain the attributes of the `phoria-island` tag a little further:

* `component` (required) - The name of the component to render, which must match a component [registered](#register-the-ui-component) via the `registerComponents` function
* `client` (optional) - The [client directive](./directives.md) to use to hydrate the component, which if not supplied implies that the component should be server rendered only
* `props` (optional) - The props to pass to the component, which can be any POCO (Plain Old CLR Object) that can be serialised to JSON and when deserialised must match the type of the component's props

> [!TIP]
> If this is just a one-off Phoria Island you may wish to stop there, but if you plan on using this component in multiple places across your app you may want to consider creating a custom Tag Helper or View Component to render the component, which we will cover in the next section.

## Render the UI component with the `PhoriaIslandComponentFactory`

Creating a custom [Tag Helper](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/intro) or [View Component](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/view-components) to render a Phoria Island component is recommended when you want to reuse the same component in multiple place across your app. It allows you to:

* Encapsulate values for attributes such as `component` (name) and `client` (directive) for consistency
* Add strongly-typed `props` so there is no chance of misspelling or mistyping prop names or values
* Encapsulate any additional business logic or validation you may need when rendering the component

To create a custom Tag Helper for the `Counter` component we can create a new class:

```csharp
public class CounterTagHelper : TagHelper
{
  private readonly IPhoriaIslandComponentFactory phoriaIslandFactory;

  public int? StartAt { get; set; }

  public CounterTagHelper(IPhoriaIslandComponentFactory phoriaIslandFactory)
  {
    this.phoriaIslandFactory = phoriaIslandFactory;
  }

  public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
  {
    var props = new CounterProps
    {
      StartAt = StartAt ?? 0
    };

    PhoriaIslandHtmlContent island = await phoriaIslandFactory.CreateAsync("Counter", props, Client.Load);

    output.TagName = null;
    output.TagMode = TagMode.SelfClosing;

    output.Content.SetHtmlContent(island);
  }
}
```

Or you can create a View Component:

```csharp
public class CounterViewComponent : ViewComponent
{
  private readonly IPhoriaIslandComponentFactory phoriaIslandFactory;

  public CounterViewComponent(IPhoriaIslandComponentFactory phoriaIslandFactory)
  {
    this.phoriaIslandFactory = phoriaIslandFactory;
  }

  public async Task<IViewComponentResult> InvokeAsync(int? startAt)
  {
    var props = new CounterProps
    {
      StartAt = startAt ?? 0
    };

    PhoriaIslandHtmlContent island = await phoriaIslandFactory.CreateAsync("Counter", props, Client.Load);

    return new HtmlContentViewComponentResult(island);
  }
}
```

> [!NOTE]
> There is no particular advantage to using a Tag Helper or View Component over the other, it is more a matter of personal/team preference or down to your particular requirements.

And a new class the model the component's props:

```csharp
public class CounterProps
{
  public int StartAt { get; set; }
}
```

> [!TIP]
> In this example we are accepting parameters to the Tag Helper and View Component that model the individual props and then constructing the `CounterProps` instance internally to create the Phoria Island, but you could also accept a single parameter that accepts an instance of `CounterProps` if you prefer.

You can then use the Tag Helper or View Component in your Razor Pages or MVC views:

```html
<!-- Tag Helper -->
<counter start-at="19" />

<!-- View Component -->
<vc:counter start-at="9" />
```

> [!TIP]
> This assumes that you have exposed the Tag Helper or View Component to your pages/views by adding the appropriate `@addTagHelper` directive to your `_ViewImports.cshtml` file.
