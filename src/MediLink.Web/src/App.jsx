import { BrowserRouter, Link, Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import { useEffect, useMemo, useState } from 'react'
import { FaCartShopping, FaMagnifyingGlass, FaPills, FaShieldHeart, FaTruckMedical } from 'react-icons/fa6'
import Login from './pages/auth/Login'
import Register from './pages/auth/Register'
import './styles.css'

const medicines = [
  { id: 1, name: 'Dolo 650 Tablet', category: 'Fever & pain', price: 32, pack: '15 tablets', icon: '💊', color: 'blue' },
  { id: 2, name: 'Cetirizine 10mg', category: 'Allergy care', price: 24, pack: '10 tablets', icon: '🌿', color: 'green' },
  { id: 3, name: 'Vitamin C 500mg', category: 'Vitamins', price: 145, pack: '30 chewable tablets', icon: '🍊', color: 'orange' },
  { id: 4, name: 'Digene Gel', category: 'Digestive care', price: 118, pack: '200 ml', icon: '🧪', color: 'purple' },
  { id: 5, name: 'ORS Electrolyte', category: 'Wellness', price: 38, pack: '200 ml', icon: '💧', color: 'teal' },
  { id: 6, name: 'Volini Spray', category: 'Pain relief', price: 210, pack: '100 g', icon: '🩹', color: 'pink' },
]

function Shell() {
  const [cart, setCart] = useState(() => JSON.parse(localStorage.getItem('medilink-cart') || '[]'))
  const [query, setQuery] = useState('')
  const [notice, setNotice] = useState('')
  const user = JSON.parse(localStorage.getItem('medilink-user') || 'null')
  useEffect(() => localStorage.setItem('medilink-cart', JSON.stringify(cart)), [cart])
  const add = (medicine) => { setCart(items => [...items, medicine]); setNotice(`${medicine.name} added to cart`); setTimeout(() => setNotice(''), 2200) }
  const filtered = useMemo(() => medicines.filter(m => `${m.name} ${m.category}`.toLowerCase().includes(query.toLowerCase())), [query])
  const logout = () => { localStorage.removeItem('medilink-user'); localStorage.removeItem('medilink-token'); location.assign('/') }
  return <>
    <header className="nav"><Link className="brand" to="/"><span>✚</span> MediLink</Link><nav><a href="#medicines">Medicines</a><a href="#how">How it works</a></nav><div className="nav-actions">{user ? <><span className="welcome">Hi, {user.fullName?.split(' ')[0]}</span><button className="link-btn" onClick={logout}>Logout</button></> : <Link className="login-link" to="/login">Login</Link>}<a className="cart" href="#cart"><FaCartShopping /> <b>{cart.length}</b></a></div></header>
    {notice && <div className="toast">✓ {notice}</div>}
    <Routes>
      <Route path="/" element={<Home query={query} setQuery={setQuery} products={filtered} add={add} />} />
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
    <Cart items={cart} setCart={setCart} />
    <footer><div className="brand"><span>✚</span> MediLink</div><p>Your reliable partner for everyday healthcare.</p><small>© 2026 MediLink. Built with care.</small></footer>
  </>
}

function Home({ query, setQuery, products, add }) { return <main>
  <section className="hero"><div className="hero-copy"><span className="eyebrow">HEALTHCARE, SIMPLIFIED</span><h1>Care that comes<br /><em>closer to you.</em></h1><p>Order genuine medicines, wellness essentials and healthcare products from trusted pharmacies.</p><div className="search"><FaMagnifyingGlass /><input value={query} onChange={e => setQuery(e.target.value)} placeholder="Search medicines, health products..." /><button>Search</button></div><div className="trust"><span>✓ Genuine products</span><span>✓ Licensed pharmacies</span><span>✓ Secure payments</span></div></div><div className="hero-art"><div className="cross">✚</div><div className="art-card card-one">💊<small>Verified medicines</small></div><div className="art-card card-two">🧑‍⚕️<small>Expert care</small></div><div className="art-card card-three">🩺<small>Health first</small></div></div></section>
  <section className="benefits"><div><FaShieldHeart /><b>100% Genuine</b><span>Sourced from verified sellers</span></div><div><FaTruckMedical /><b>Fast delivery</b><span>At your doorstep, on time</span></div><div><FaPills /><b>Wide selection</b><span>All your health needs in one place</span></div></section>
  <section className="catalog" id="medicines"><div className="section-heading"><div><span className="eyebrow">SHOP BY CATEGORY</span><h2>Find what you need</h2></div><a href="#medicines">View all medicines →</a></div><div className="categories">{['Fever & pain','Diabetes care','Vitamins','Personal care','Baby care','Ayurveda'].map((x,i)=><button key={x} onClick={()=>setQuery(x)}><span>{['💊','🩸','🍊','✨','🧸','🌿'][i]}</span>{x}</button>)}</div></section>
  <section className="catalog"><div className="section-heading"><div><span className="eyebrow">POPULAR PRODUCTS</span><h2>Everyday health essentials</h2></div></div><div className="products">{products.map(m=><article className="product" key={m.id}><div className={`product-icon ${m.color}`}>{m.icon}<span>In stock</span></div><p>{m.category}</p><h3>{m.name}</h3><small>{m.pack}</small><div className="price"><b>₹{m.price}</b><button onClick={()=>add(m)}>Add +</button></div></article>)}</div>{products.length===0&&<p className="empty">No medicines found. Try a different search.</p>}</section>
  <section className="steps" id="how"><span className="eyebrow">HOW MEDILINK WORKS</span><h2>Healthcare in three easy steps</h2><div><article><b>1</b><h3>Search & select</h3><p>Find medicines from a wide, trusted catalogue.</p></article><article><b>2</b><h3>Add to cart</h3><p>Review your essentials and place your order securely.</p></article><article><b>3</b><h3>Get it delivered</h3><p>Receive your healthcare products at your doorstep.</p></article></div></section>
 </main> }

function Cart({items,setCart}) { const total=items.reduce((n,x)=>n+x.price,0); return <aside className="cart-drawer" id="cart"><h3>Your cart <small>{items.length} item(s)</small></h3>{items.length ? <>{items.map((x,i)=><div className="cart-row" key={`${x.id}-${i}`}><span>{x.icon}</span><div>{x.name}<small>₹{x.price}</small></div><button onClick={()=>setCart(items.filter((_,index)=>index!==i))}>×</button></div>)}<div className="total">Total <b>₹{total}</b></div><button className="checkout">Proceed to checkout</button></> : <p>Your cart is empty. Add products to get started.</p>}</aside> }
export default function App(){ return <BrowserRouter><Shell /></BrowserRouter> }
