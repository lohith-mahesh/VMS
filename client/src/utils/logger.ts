import axios from 'axios'

const sensitiveKeys = /password|token|secret|database_url|authorization|idnumber|passport/i

function sanitize(value: unknown): unknown {
  if (value instanceof Error) return { name: value.name, message: value.message }
  if (Array.isArray(value)) return value.map(sanitize)
  if (value && typeof value === 'object') return Object.fromEntries(Object.entries(value).filter(([key]) => !sensitiveKeys.test(key)).map(([key, item]) => [key, sanitize(item)]))
  return value
}

export const logger = {
  info(message: string, context?: unknown) { if (import.meta.env.DEV) console.info(`[RRVMS] ${message}`, sanitize(context)) },
  warn(message: string, context?: unknown) { if (import.meta.env.DEV) console.warn(`[RRVMS] ${message}`, sanitize(context)) },
  error(message: string, context?: unknown) { if (import.meta.env.DEV) console.error(`[RRVMS] ${message}`, sanitize(context)) },
}

export function userFacingApiError(error: unknown, fallback: string) {
  logger.error(fallback, axios.isAxiosError(error) ? { status: error.response?.status, message: error.message } : error)
  if (!error || !axios.isAxiosError(error)) return fallback
  if (!error.response) return 'Unable to connect to RRVMS services.'
  if (error.response.status === 401) return 'Your session has expired. Please sign in again.'
  if (error.response.status === 403) return 'You do not have permission to perform this action.'
  if (error.response.status >= 500) return 'RRVMS services are temporarily unavailable.'
  return fallback
}