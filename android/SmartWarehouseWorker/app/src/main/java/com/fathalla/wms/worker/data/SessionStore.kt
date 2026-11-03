package com.fathalla.wms.worker.data

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map

private val Context.dataStore by preferencesDataStore("wms_session")

class SessionStore(private val context: Context) {
    private val tokenKey = stringPreferencesKey("token")
    private val nameKey = stringPreferencesKey("name")
    private val baseUrlKey = stringPreferencesKey("baseUrl")

    suspend fun token(): String? = context.dataStore.data.map { it[tokenKey] }.first()
    suspend fun name(): String? = context.dataStore.data.map { it[nameKey] }.first()
    suspend fun baseUrl(): String = context.dataStore.data.map { it[baseUrlKey] }.first()
        ?: "http://10.0.2.2:5138/"

    suspend fun save(token: String, name: String, baseUrl: String) {
        context.dataStore.edit {
            it[tokenKey] = token
            it[nameKey] = name
            it[baseUrlKey] = baseUrl.trimEnd('/') + "/"
        }
    }

    suspend fun clear() {
        context.dataStore.edit {
            it.remove(tokenKey)
            it.remove(nameKey)
        }
    }
}
