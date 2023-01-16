module.exports = {
  edi: {
    sqlConfig: {
      user: 'sa',
      password: 'xV5nK3vX1eX6nU3j',
      database: 'EDI',
      server: '171.244.136.172',
      port: 12154,
      pool: {
        max: 10,
        min: 0,
        idleTimeoutMillis: 30000
      },
      options: {
        encrypt: false, // for azure
        trustServerCertificate: false // change to true for local dev / self-signed certs
      }
    },
    sqlite: './sql/app.db'
  },
  tms: {
    wr1: {
      sql: {
        user: 'sa',
        password: 'xV5nK3vX1eX6nU3j',
        database: 'tms_wr1',
        server: '171.244.136.172',
        port: 12154,
        pool: {
          max: 10,
          min: 0,
          idleTimeoutMillis: 30000
        },
        options: {
          encrypt: false, // for azure
          trustServerCertificate: false // change to true for local dev / self-signed certs
        }
      },
      portal: {
        signIn: 'https://wr1.com.vn/api/cont/cust/user/login',
        username: 'tms_api',
        password: 'Tms@2022#$',
      },
      fast: {
        sqlConfig: {
          user: 'WR130112021',
          password: 'dhqgtphcm',
          database: 'FASTPROV5WR1_DB',
          server: 'FAST.WR1.ASIA',
          port: 2038,
          pool: {
            max: 10,
            min: 0,
            idleTimeoutMillis: 30000
          },
          options: {
            encrypt: false, // for azure
            trustServerCertificate: false // change to true for local dev / self-signed certs
          }
        }
      }
    }
  }
}