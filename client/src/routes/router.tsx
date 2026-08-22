import { createBrowserRouter } from 'react-router-dom'
import { BaseLayout } from '../layouts/BaseLayout'
import { HomePage } from '../pages/HomePage'

export const router = createBrowserRouter([
  { path: '/', element: <BaseLayout />, children: [{ index: true, element: <HomePage /> }] },
])
