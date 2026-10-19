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
| `src/pages` | One file per route: `Home`, `Product`, `AuthPage`, `Checkout`, `Orders`, `OrderDetail` |
| `src/components` | Pieces used by several pages: `Navbar`, `Cart`, `Footer`, `ProductCard`, `QuantityStepper`, `TextField`, `Picture`, `OrderParts`, `Logo`, `Layout`, `RequireAuth` |
| `src/context` | Shared state: `AuthContext` (who is signed in), `CartContext` (the server cart), `CatalogContext` (the products) |
| `src/hooks` | `useAddToCart`, used by the product cards and the product page |
| `src/lib` | `api.js` (every call to the API), `format.js` (prices), `navigation.js` (the main links and categories) |
| `src/data/data.json` | Product text and pictures |

## Routes

| Path | Page | Signed in |
|---|---|---|
| `/` | Storefront: hero, filters, product grid | no |
| `/headphones`, `/speakers`, `/earphones` | The same storefront with that category selected | no |
| `/:category/:slug` | Product page | no |
| `/login`, `/register` | One card for both | no |
| `/checkout` | Address, payment method, summary | yes |
| `/orders` | My orders with status tabs | yes |
| `/orders/:id` | One order: progress, items, summary, cancel | yes |

The signed in routes sit under one `RequireAuth` route in `App.jsx`, which sends visitors to `/login?redirect=…`.

## Where the data comes from

- **Products:** `CatalogContext` joins `data.json` (name, text, pictures, gallery, *in the box*) with the API's
  `GET /products` (id, price, stock) on the slug. Pages only see finished products like
  `{ name, image, price, stock, inStock, id }`. A product the API does not sell has `id: null` and shows *Not available*.
- **Cart:** `CartContext` keeps the server cart. Every cart call returns the whole cart, so the context just stores
  the answer. Totals and the shipping fee come from the API.
- **Orders:** the order pages call the API directly. The order page polls every 3 seconds while a card payment waits
  for Fawaterak's webhook.
- **Session:** `lib/api.js` keeps the tokens in `localStorage`, adds the bearer token, and on a 401 trades the
  refresh token once for a new pair.

## Design system

The Stitch theme has about 40 colors. The shop uses 10 of them, named for what they do, in `tailwind.config.js`:

| Token | Color | Used for |
|---|---|---|
| `brand` | `#d87d4a` | Buttons, active filters, the logo dot, prices that matter |
| `brand-hover` | `#c06a39` | Button hover |
| `brand-dark` | `#95491a` | Orange text: eyebrows, active links |
| `brand-soft` | `#fbeee6` | Selected payment method, confirmed badge |
| `ink` | `#1b1c1c` | Text |
| `muted` | `#5f5e5e` | Secondary text |
| `page` | `#fbf9f9` | Page background |
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

Left out on purpose because the API has no data for it: star ratings, wishlist, sale prices, the DAC and
accessory categories, price and sort filters, pagination, Google and Apple login, forgot password, shipment
tracking numbers, loyalty points, several saved addresses, and the newsletter form. Card number fields were also
left out: the card is always typed on Fawaterak's page, never on ours. The admin screens wait for the admin API.
