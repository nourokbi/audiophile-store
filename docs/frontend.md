# Frontend

The shop in `client/`: React 18, Vite, Tailwind CSS 3 and [Lucide](https://lucide.dev) icons. The look comes from
the light screens of the Stitch project *Audiophile E-commerce Hub*, rebuilt as React components and connected to
the API. Nothing else is installed: no component library and no state library, because the shop is small enough
for React context and a few shared classes.

## Run it

```bash
cd client
npm install
npm run dev     # http://localhost:5173, /api and /payments are forwarded to the API on port 5040
npm run lint    # must pass with zero warnings
npm run build
```

After a change to `tailwind.config.js`, restart `npm run dev`: the dev server does not reload the config by itself.

## Folders

| Folder | What lives there |
|---|---|
| `src/pages` | One file per route: `Home`, `Product`, `Cart`, `AuthPage`, `Checkout`, `Orders`, `OrderDetail`, `Settings`, `AdminDashboard` |
| `src/components` | Pieces used by several pages: `Navbar`, `UserDrawer`, `Footer`, `ProductCard`, `QuantityStepper`, `TextField`, `Picture`, `OrderParts`, `Logo`, `Layout`, `RequireAuth` |
| `src/context` | Shared state: `ThemeContext` (light or dark), `AuthContext` (who is signed in), `CartContext` (the server cart), `CatalogContext` (the products) |
| `src/hooks` | `useAddToCart`, used by the product cards and the product page |
| `src/lib` | `api.js` (every call to the API), `format.js` (prices), `navigation.js` (the categories with their icons), `shipping.js` (the free shipping threshold shown on the cart page) |
| `src/data/pictures.json` | Each product's photo, and for the six original products the responsive sizes and the gallery |
| `src/data/adminSample.js` | Sample numbers for the admin dashboard until the admin API exists |

## Routes

| Path | Page | Signed in |
|---|---|---|
| `/` | Storefront: hero, filters, product grid | no |
| `/new` | The new collection, the same storefront showing only new products | no |
| `/headphones`, `/speakers`, `/earphones`, `/amplifiers`, `/accessories` | The same storefront with that category selected | no |
| `/:category/:slug` | Product page | no |
| `/cart` | The cart with quantities, remove, totals and the way to checkout | no, it asks to log in |
| `/login`, `/register` | One card for both | no |
| `/checkout` | Pick a saved address or add a new one, payment method, summary | yes |
| `/orders` | My orders with status tabs | yes |
| `/orders/:id` | One order: progress, items, summary, cancel | yes |
| `/settings` | Profile, light or dark theme, saved addresses | yes |
| `/admin` | Admin dashboard | admin only |

The signed in routes sit under one `RequireAuth` route in `App.jsx`, which sends visitors to `/login?redirect=…`.
`/admin` sits under `RequireAuth role='Admin'`, which also sends customers back to the home page.

## Header and user drawer

The header has the logo on the left, **Home**, **Categories** (opens on hover with the five categories) and
**New collection** in the middle, and the cart and the account button on the right. The cart icon opens `/cart`. The account button opens the
**user drawer** from the right: My orders, Admin dashboard (admins only), Settings, the dark theme switch and Log
out. Visitors who are not signed in get Log in, Create account and the theme switch.

## Storefront filters

Category chips, a search box, a price range (under 1,000, 1,000 to 3,000, 3,000 to 5,000, over 5,000 EGP), a sort
(featured, price both ways, name) and *in stock only*, with nine products per page and previous, page number and
next buttons under the grid. Changing a filter or a category goes back to page one. The filtering runs in the browser over the whole catalogue,
which is fine for a shop this size; with hundreds of products it moves to the API's `search`, `minPrice` and
`maxPrice` parameters.

## Where the data comes from

- **Products:** the API is the source of every product: name, text, price, stock, category and *in the box*.
  `CatalogContext` loads `GET /products` once and adds the photo, the gallery and the *new* flag from
  `pictures.json` by slug. A product with no entry there uses the API's `imageUrl`. Adding a product therefore
  means a seed row (or the admin API later) and, for a nicer photo, one line in `pictures.json`.
- **Cart:** `CartContext` keeps the server cart. Every cart call returns the whole cart, so the context just stores
  the answer. Totals and the shipping fee come from the API.
- **Orders:** the order pages call the API directly. The order page polls every 3 seconds while a card payment waits
  for Fawaterak's webhook.
- **Session:** `lib/api.js` keeps the tokens in `localStorage`, adds the bearer token, and on a 401 trades the
  refresh token once for a new pair.

## Design system

The Stitch theme has about 40 colors. The shop uses 11 of them, named for what they do. Each Tailwind color reads
a CSS variable, and `index.css` sets the variables twice: once for light under `:root` and once for dark under
`.dark`. Switching the theme only adds or removes the `dark` class on `<html>` (`ThemeContext`), so components
never need `dark:` variants. A small script in `index.html` applies the saved theme before the first paint.

The light values (the dark ones are in `index.css`):

| Token | Color | Used for |
|---|---|---|
| `brand` | `#d87d4a` | Buttons, active filters, the logo dot, prices that matter |
| `brand-hover` | `#c06a39` | Button hover |
| `brand-dark` | `#95491a` | Orange text: eyebrows, active links |
| `brand-soft` | `#fbeee6` | Selected payment method, confirmed badge |
| `ink` | `#1b1c1c` | Text |
| `muted` | `#5f5e5e` | Secondary text |
| `page` | `#fbf9f9` | Page background |
| `surface` | `#ffffff` | Cards, the header, the drawer |
| `panel` | `#f5f3f3` | Inputs, steppers, grey blocks inside cards |
| `panel-strong` | `#e9e8e7` | Hover on grey blocks |
| `line` | `#e4e2e2` | Dividers |

The repeated building blocks are classes in `src/index.css`, so the JSX says what a thing is, not how it looks:

| Class | What it is |
|---|---|
| `page` | The centred 1110px column with side padding |
| `card` | White box with rounded corners and a soft shadow |
| `btn-primary`, `btn-secondary`, `btn-dark` | Buttons (orange, grey, black), all 48px tall with uppercase labels |
| `input` | Grey text field with an orange focus ring |
| `label` | Small bold uppercase text |
| `eyebrow` | The spaced orange text above a title |
| `title` | A page title |
| `chip` | A filter or tab button |

Font: Manrope. Icons: `lucide-react`, imported one by one (`import { ShoppingBag } from 'lucide-react'`).

## What the design shows that the shop does not have (yet)

Left out on purpose because the API has no data for it: star ratings, wishlist, sale prices, pagination, Google and Apple login, forgot password, shipment
tracking numbers, loyalty points, several saved addresses, and the newsletter form. Card number fields were also
left out: the card is always typed on Fawaterak's page, never on ours.

## Admin dashboard

`/admin` shows four stat cards, an orders chart, a low stock list, recent orders and every product. The products,
stock and low stock list are live from the catalogue; revenue, the chart and recent orders come from
`src/data/adminSample.js`, and a banner on the page says so. When the admin API exists, each sample value is
replaced by its call: `GET /admin/stats`, `GET /admin/orders`, and the product buttons by the admin product endpoints.
